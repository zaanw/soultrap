using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Soultrap.Core;
using Soultrap.Infrastructure;
using Xunit;
namespace Soultrap.Tests;
public sealed class EnvironmentTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"soultrap-tests-"+Guid.NewGuid().ToString("N"));
    private AppStorage Storage=>new(Path.Combine(root,"data"));
    public EnvironmentTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    private static byte[] Png(byte marker=0)
    {
        var years=new[]{2013,2015,2017,2019};
        return File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory,"TestAssets",years[marker%years.Length]+".png"));
    }
    private ClientInstallation Client(string version="version-test")
    {
        var dir=Path.Combine(root,version);Directory.CreateDirectory(dir);
        foreach(var target in SafePaths.CursorTargets){var file=SafePaths.Resolve(dir,target);Directory.CreateDirectory(Path.GetDirectoryName(file)!);File.WriteAllBytes(file,Png());}
        return new(dir,version,DateTime.UtcNow);
    }
    private CustomizationEngine Engine=>new(Path.Combine(root,"backups"));
    [Fact] public async Task ConfigRoundTrip()
    {var expected=new Settings{Theme="Retro Green",Opacity=.85,AutoStart=true};await Storage.SaveSettingsAsync(expected);var actual=await Storage.LoadSettingsAsync();Assert.Equal(expected.Theme,actual.Theme);Assert.Equal(expected.Opacity,actual.Opacity);Assert.True(actual.AutoStart);}
    [Fact] public async Task MissingConfigUsesDefaults()=>Assert.Equal("Void Dark",(await Storage.LoadSettingsAsync()).Theme);
    [Fact] public async Task CorruptConfigIsPreserved()
    {Directory.CreateDirectory(Storage.Root);await File.WriteAllTextAsync(Storage.SettingsFile,"{");await Assert.ThrowsAsync<JsonException>(()=>Storage.LoadSettingsAsync());Assert.Equal("{",await File.ReadAllTextAsync(Storage.SettingsFile));}
    [Theory][InlineData(.1)][InlineData(1.1)][InlineData(double.NaN)] public async Task RejectInvalidOpacity(double opacity)=>await Assert.ThrowsAsync<InvalidDataException>(()=>Storage.SaveSettingsAsync(new Settings{Opacity=opacity}));
    [Fact] public async Task PresetImportExport()
    {var p=Catalog.BuiltInPresets().First();var id=await Storage.SavePresetAsync(p);var actual=Assert.Single(await Storage.ListPresetsAsync());Assert.Equal(id,actual.Id);Assert.Equal(p.Name,actual.Value.Name);Assert.Equal(p.Cursor,actual.Value.Cursor);Storage.DeletePreset(id);Assert.Empty(await Storage.ListPresetsAsync());}
    [Fact] public void UnsupportedPresetSchemaRejected()=>Assert.Throws<InvalidDataException>(()=>new Preset{Schema=2}.Validate());
    [Fact] public void UnknownCursorRejected()=>Assert.Throws<InvalidDataException>(()=>new Preset{Cursor="forged"}.Validate());
    [Theory][InlineData("../outside")][InlineData("/absolute")][InlineData("C:/file")][InlineData("content/../../outside")][InlineData("a\\b")][InlineData("a//b")][InlineData("a/./b")]
    public void RejectUnsafePaths(string path)=>Assert.Throws<InvalidDataException>(()=>SafePaths.Resolve(root,path));
    [Fact] public void RejectSymbolicLink()
    {var outside=Path.Combine(root,"outside");Directory.CreateDirectory(outside);Directory.CreateSymbolicLink(Path.Combine(root,"link"),outside);Assert.Throws<InvalidDataException>(()=>SafePaths.Resolve(root,"link/file"));}
    [Fact] public void NormalPathAccepted()=>Assert.Equal(Path.Combine(root,"content","safe.png"),SafePaths.Resolve(root,"content/safe.png"));
    [Fact] public void CompatibilityNeedsBothCursorFiles()
    {var client=Client();Assert.True(CustomizationEngine.Check(client).CanApply);File.Delete(SafePaths.Resolve(client.Root,SafePaths.CursorTargets[1]));Assert.False(CustomizationEngine.Check(client).CanApply);}
    [Fact] public async Task ApplyVerifyAndRollback()
    {
        var client=Client();var engine=Engine;var journal=await engine.ApplyAsync(client,SafePaths.CursorTargets.ToDictionary(t=>t,t=>Png(1)),"cursor");
        foreach(var target in SafePaths.CursorTargets)Assert.Equal(Png(1),await File.ReadAllBytesAsync(SafePaths.Resolve(client.Root,target)));
        Assert.Single(await engine.ListAsync());await engine.RestoreAsync(client,journal);
        foreach(var target in SafePaths.CursorTargets)Assert.Equal(Png(),await File.ReadAllBytesAsync(SafePaths.Resolve(client.Root,target)));
        Assert.True(Assert.Single(await engine.ListAsync()).Restored);
    }
    [Fact] public async Task UndoInReverseOrder()
    {var c=Client();var e=Engine;var first=await e.ApplyAsync(c,new Dictionary<string,byte[]>{{SafePaths.CursorTargets[0],Png(1)}},"one");var second=await e.ApplyAsync(c,new Dictionary<string,byte[]>{{SafePaths.CursorTargets[0],Png(2)}},"two");await Assert.ThrowsAsync<IOException>(()=>e.RestoreAsync(c,first));await e.RestoreAsync(c,second);await e.RestoreAsync(c,first);Assert.Equal(Png(),File.ReadAllBytes(SafePaths.Resolve(c.Root,SafePaths.CursorTargets[0])));}
    [Fact] public async Task VersionMismatchDoesNotWrite()
    {var c=Client();var e=Engine;var j=await e.ApplyAsync(c,new Dictionary<string,byte[]>{{SafePaths.CursorTargets[0],Png(1)}},"test");await Assert.ThrowsAsync<InvalidDataException>(()=>e.RestoreAsync(Client("version-new"),j));Assert.Equal(Png(1),File.ReadAllBytes(SafePaths.Resolve(c.Root,SafePaths.CursorTargets[0])));}
    [Fact] public async Task RestoreRejectsExternalChangesBeforeWritingAnyFile()
    {var c=Client();var e=Engine;var j=await e.ApplyAsync(c,SafePaths.CursorTargets.ToDictionary(t=>t,t=>Png(1)),"test");File.WriteAllBytes(SafePaths.Resolve(c.Root,SafePaths.CursorTargets[1]),Png(2));await Assert.ThrowsAsync<IOException>(()=>e.RestoreAsync(c,j));Assert.Equal(Png(1),File.ReadAllBytes(SafePaths.Resolve(c.Root,SafePaths.CursorTargets[0])));}
    [Fact] public async Task TamperedBackupRejected()
    {var c=Client();var e=Engine;var j=await e.ApplyAsync(c,new Dictionary<string,byte[]>{{SafePaths.CursorTargets[0],Png(1)}},"test");File.WriteAllBytes(Path.Combine(root,"backups",j.Id,"0.bin"),Png(9));await Assert.ThrowsAsync<InvalidDataException>(()=>e.RestoreAsync(c,j));}
    [Fact] public async Task UnsupportedModTargetDoesNotWrite()
    {var c=Client();await Assert.ThrowsAsync<InvalidDataException>(()=>Engine.ApplyAsync(c,new Dictionary<string,byte[]>{{"RobloxPlayerBeta.exe",Png()}},"unsafe"));Assert.False(File.Exists(Path.Combine(c.Root,"RobloxPlayerBeta.exe")));}
    [Fact] public async Task ActiveBackupCannotBeDeleted()
    {var c=Client();var e=Engine;var j=await e.ApplyAsync(c,new Dictionary<string,byte[]>{{SafePaths.CursorTargets[0],Png(1)}},"test");Assert.Throws<InvalidOperationException>(()=>e.Delete(j));await e.RestoreAsync(c,j);e.Delete(j);Assert.Empty(await e.ListAsync());}
    [Fact] public void InvalidPngRejected()=>Assert.Throws<InvalidDataException>(()=>SafePaths.ValidatePng([1,2,3]));
    [Fact] public void OversizedPngDimensionsRejected(){var bytes=Png();bytes[18]=2;Assert.Throws<InvalidDataException>(()=>SafePaths.ValidatePng(bytes));}
    [Fact] public void FlagValidationDistinguishesTypes()
    {FlagPolicy.Validate(JsonSerializer.Deserialize<Dictionary<string,JsonElement>>("{\"FFlagTest\":true,\"DFIntTest\":4}")!);Assert.Throws<InvalidDataException>(()=>FlagPolicy.Validate(JsonSerializer.Deserialize<Dictionary<string,JsonElement>>("{\"FFlagTest\":7}")!));}
    [Fact] public void ObjectsAreNotFlagScalars()=>Assert.Throws<InvalidDataException>(()=>FlagPolicy.Validate(JsonSerializer.Deserialize<Dictionary<string,JsonElement>>("{\"FStringTest\":{}}")!));
    [Theory][InlineData("v0.2.0",true)][InlineData("0.1.0",false)][InlineData("0.0.9",false)] public void ReleaseVersionComparison(string tag,bool newer)=>Assert.Equal(newer,UpdateService.IsNewer(tag,new Version(0,1,0)));
    [Fact] public void InvalidReleaseTagRejected()=>Assert.Throws<InvalidDataException>(()=>UpdateService.ParseVersion("latest"));
    private sealed class Handler(Func<HttpRequestMessage,HttpResponseMessage> respond):HttpMessageHandler
    {protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken cancellationToken)=>Task.FromResult(respond(request));}
    [Fact] public async Task NoPublishedReleaseIsExplicit()
    {using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.NotFound)));await Assert.ThrowsAsync<InvalidOperationException>(()=>new UpdateService(http).CheckAsync());}
    [Fact] public async Task DownloadChecksHashBeforeReplacingExistingFile()
    {var file=Path.Combine(root,"release.zip");File.WriteAllText(file,"existing");using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent([1,2,3])}));var service=new UpdateService(http);var asset=new ReleaseAsset("app.zip","https://github.com/zaanw/soultrap/releases/download/v0.2.0/app.zip","sha256:"+new string('0',64));await Assert.ThrowsAsync<InvalidDataException>(()=>service.DownloadAsync(asset,file));Assert.Equal("existing",File.ReadAllText(file));Assert.Empty(Directory.GetFiles(root,"*.download"));}
    [Fact] public async Task DownloadRequiresDigest()
    {using var http=new HttpClient();await Assert.ThrowsAsync<InvalidDataException>(()=>new UpdateService(http).DownloadAsync(new("a.zip","https://github.com/zaanw/soultrap/releases/download/v0.2.0/a.zip",null),Path.Combine(root,"a.zip")));}
    [Fact] public async Task RejectUnexpectedUpdateHost()
    {using var http=new HttpClient();await Assert.ThrowsAsync<InvalidDataException>(()=>new UpdateService(http).DownloadAsync(new("a.zip","https://example.com/a.zip",null),Path.Combine(root,"a.zip")));}
    [Fact] public async Task ValidDownloadVerified()
    {byte[] bytes=[4,5,6];using var http=new HttpClient(new Handler(_=>new(HttpStatusCode.OK){Content=new ByteArrayContent(bytes)}));var dest=Path.Combine(root,"a.zip");await new UpdateService(http).DownloadAsync(new("a.zip","https://github.com/zaanw/soultrap/releases/download/v0.2.0/a.zip","sha256:"+CustomizationEngine.Hash(bytes)),dest);Assert.Equal(bytes,File.ReadAllBytes(dest));}
    [Fact] public async Task ZipSlipRejected()
    {var zip=Path.Combine(root,"mod.zip");using(var a=ZipFile.Open(zip,ZipArchiveMode.Create)){a.CreateEntry("mod.json");a.CreateEntry("../escape.png");}await Assert.ThrowsAsync<InvalidDataException>(()=>ModPackages.ReadAsync(zip,"version-test"));}
    [Fact] public async Task ModManifestVersionEnforced()
    {var zip=await MakeMod("version-old");await Assert.ThrowsAsync<InvalidDataException>(()=>ModPackages.ReadAsync(zip,"version-test"));}
    [Fact] public async Task CompatiblePackageImports()
    {var mod=await ModPackages.ReadAsync(await MakeMod("version-test"),"version-test");Assert.Equal("Test",mod.Manifest.Name);Assert.Equal(Png(1),mod.Files[SafePaths.CursorTargets[0]]);}
    private async Task<string> MakeMod(string version)
    {
        var path=Path.Combine(root,Guid.NewGuid()+".zip");using var zip=ZipFile.Open(path,ZipArchiveMode.Create);
        await using(var output=zip.CreateEntry("mod.json").Open())await JsonSerializer.SerializeAsync(output,new ModManifest(1,"Test",version,new(){{SafePaths.CursorTargets[0],"cursor.png"}}),JsonFiles.Options);
        await using(var output=zip.CreateEntry("cursor.png").Open())await output.WriteAsync(Png(1));return path;
    }
}
