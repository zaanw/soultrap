using System.Buffers.Binary;
using System.Net;
using System.Text.Json;
using Soultrap.Infrastructure;
using Xunit;
namespace Soultrap.Tests;
public sealed class UpdaterTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"Soultrap-updater-"+Guid.NewGuid().ToString("N"));
    public UpdaterTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    private static byte[] Executable()
    {
        // PE-header fixture for validation only. It is never executed.
        var bytes=new byte[1024];bytes[0]=(byte)'M';bytes[1]=(byte)'Z';BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(60),128);
        "PE\0\0"u8.CopyTo(bytes.AsSpan(128));BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(132),0x8664);BinaryPrimitives.WriteUInt16LittleEndian(bytes.AsSpan(150),2);return bytes;
    }
    private static ReleaseInfo Release(byte[]? bytes=null)
    {
        bytes??=Executable();return new("v0.3.0","Changes","https://github.com/zaanw/soultrap/releases/tag/v0.3.0",[new("Soultrap-0.3.0-Setup-x64.exe","https://github.com/zaanw/soultrap/releases/download/v0.3.0/Soultrap-0.3.0-Setup-x64.exe","sha256:"+CustomizationEngine.Hash(bytes))]);
    }
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> respond):HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)
        {var response=respond(request);response.RequestMessage??=request;return Task.FromResult(response);}
    }
    private sealed class ProgressSink: IProgress<DownloadProgress>
    {public List<DownloadProgress> Values{get;}=[];public void Report(DownloadProgress value)=>Values.Add(value);}
    [Fact] public void ThreeAndFourPartVersionsAreEqual()=>Assert.False(UpdateService.IsNewer("v0.3.0",new Version(0,3,0,0)));
    [Theory][InlineData("v0.3.0-beta")][InlineData("../0.3.0")][InlineData("3")][InlineData("0.3.0/evil")]
    public void InvalidStableTagsRejected(string tag)=>Assert.Throws<InvalidDataException>(()=>UpdateService.ParseVersion(tag));
    [Fact] public void DraftNotAnUpdate()=>Assert.Throws<InvalidDataException>(()=>UpdateService.SelectInstaller(Release() with{Draft=true}));
    [Fact] public void PrereleaseNotAnUpdate()=>Assert.Throws<InvalidDataException>(()=>UpdateService.SelectInstaller(Release() with{Prerelease=true}));
    [Fact] public void ForeignReleaseRejected()=>Assert.Throws<InvalidDataException>(()=>UpdateService.SelectInstaller(Release() with{Url="https://github.com/other/app/releases/tag/v0.3.0"}));
    [Fact] public void DuplicateInstallerRejected(){var r=Release();Assert.Throws<InvalidDataException>(()=>UpdateService.SelectInstaller(r with{Assets=[r.Assets[0],r.Assets[0]]}));}
    [Fact] public void WrongVersionInstallerRejected(){var r=Release();Assert.Throws<InvalidDataException>(()=>UpdateService.SelectInstaller(r with{Assets=[r.Assets[0] with{Name="Soultrap-0.2.0-Setup-x64.exe"}]}));}
    [Fact] public void WrongTagAssetUrlRejected(){var r=Release();Assert.Throws<InvalidDataException>(()=>UpdateService.SelectInstaller(r with{Assets=[r.Assets[0] with{Url=r.Assets[0].Url.Replace("/v0.3.0/","/v0.2.0/")}]}));}
    [Fact] public void MissingAssetDigestRejected(){var r=Release();Assert.Throws<InvalidDataException>(()=>UpdateService.SelectInstaller(r with{Assets=[r.Assets[0] with{Digest=null}]}));}
    [Fact] public async Task CheckReadsActualGitHubJsonFields()
    {
        var json="{\"tag_name\":\"v0.3.0\",\"body\":\"Release notes\",\"html_url\":\"https://github.com/zaanw/soultrap/releases/tag/v0.3.0\",\"draft\":false,\"prerelease\":false,\"assets\":[{\"name\":\"Soultrap-0.3.0-Setup-x64.exe\",\"browser_download_url\":\"https://github.com/zaanw/soultrap/releases/download/v0.3.0/Soultrap-0.3.0-Setup-x64.exe\",\"digest\":\"sha256:"+CustomizationEngine.Hash(Executable())+"\"}]}";
        using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new StringContent(json)}));var r=await new UpdateService(http).CheckAsync();Assert.Equal("Release notes",r.Notes);Assert.EndsWith(".exe",UpdateService.SelectInstaller(r).Url);
    }
    [Fact] public async Task PrepareDownloadsAndRechecksExecutable()
    {
        var bytes=Executable();using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)}));var progress=new ProgressSink();
        var prepared=await new UpdateService(http).PrepareInstallerAsync(Release(),new Version(0,2,1),root,progress);
        Assert.Equal(bytes,File.ReadAllBytes(prepared.Path));Assert.EndsWith("Installer verified — awaiting confirmation",progress.Values.Last().Stage);Assert.Equal(100,progress.Values.Last().Percent);
        await using var locked=await UpdateService.OpenVerifiedInstallerAsync(prepared);Assert.Equal(bytes.Length,locked.Length);
    }
    [Fact] public async Task DowngradeRefusedBeforeNetwork()
    {using var http=new HttpClient(new Handler(_=>throw new Exception("Should not request")));await Assert.ThrowsAsync<InvalidOperationException>(()=>new UpdateService(http).PrepareInstallerAsync(Release(),new Version(0,4,0),root));}
    [Fact] public async Task NonExecutableDigestDoesNotAuthorizeInstallation()
    {byte[] bytes=[1,2,3];using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)}));await Assert.ThrowsAnyAsync<IOException>(()=>new UpdateService(http).PrepareInstallerAsync(Release(bytes),new Version(0,2,1),root));}
    [Fact] public async Task TamperedCacheRejectedBeforeLaunch()
    {
        var bytes=Executable();using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)}));var prepared=await new UpdateService(http).PrepareInstallerAsync(Release(),new Version(0,2,1),root);
        bytes[^1]=99;File.WriteAllBytes(prepared.Path,bytes);await Assert.ThrowsAsync<InvalidDataException>(()=>UpdateService.OpenVerifiedInstallerAsync(prepared));
    }
    [Fact] public async Task ConfirmationCannotBeOmitted()=>await Assert.ThrowsAsync<InvalidOperationException>(()=>UpdateService.InstallAsync(null!,root,userConfirmed:false));
    [Fact] public async Task CancelledDownloadDoesNotCreateInstaller()
    {using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(Executable())}));using var cancellation=new CancellationTokenSource();cancellation.Cancel();await Assert.ThrowsAnyAsync<OperationCanceledException>(()=>new UpdateService(http).PrepareInstallerAsync(Release(),new Version(0,2,1),root,ct:cancellation.Token));Assert.Empty(Directory.GetFiles(root,"*.exe",SearchOption.AllDirectories));}
    [Fact] public async Task ForeignRedirectRejected()
    {
        using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(Executable()),RequestMessage=new HttpRequestMessage(HttpMethod.Get,"https://example.com/payload.exe")}));
        await Assert.ThrowsAsync<InvalidDataException>(()=>new UpdateService(http).PrepareInstallerAsync(Release(),new Version(0,2,1),root));
    }
    [Fact] public async Task HeaderSizeLimitCheckedBeforeWriting()
    {
        using var http=new HttpClient(new Handler(_=>{var content=new ByteArrayContent(Executable());content.Headers.ContentLength=300_000_001;return new(HttpStatusCode.OK){Content=content};}));
        await Assert.ThrowsAsync<InvalidDataException>(()=>new UpdateService(http).PrepareInstallerAsync(Release(),new Version(0,2,1),root));Assert.Empty(Directory.GetFiles(root,"*.download",SearchOption.AllDirectories));
    }
}
