using System.Buffers.Binary;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
namespace Soultrap.Infrastructure;
public sealed record ReleaseAsset([property:JsonPropertyName("name")]string Name, [property:JsonPropertyName("browser_download_url")]string Url, [property:JsonPropertyName("digest")]string? Digest);
public sealed record ReleaseInfo([property:JsonPropertyName("tag_name")]string Tag, [property:JsonPropertyName("body")]string Notes, [property:JsonPropertyName("html_url")]string Url, [property:JsonPropertyName("assets")]ReleaseAsset[] Assets)
{
    [JsonPropertyName("draft")]public bool Draft {get;init;}
    [JsonPropertyName("prerelease")]public bool Prerelease {get;init;}
}
public sealed record DownloadProgress(long Bytes,long? Total,string Stage)
{
    public double Percent=>Total is >0?Math.Clamp(Bytes*100d/Total.Value,0,100):0;
}
public sealed record PreparedInstaller(string Path,ReleaseInfo Release,ReleaseAsset Asset);
public sealed class UpdateService(HttpClient http)
{
    public async Task<ReleaseInfo> CheckAsync(CancellationToken ct=default)
    {
        using var req=new HttpRequestMessage(HttpMethod.Get,"https://api.github.com/repos/zaanw/soultrap/releases/latest");
        req.Headers.UserAgent.ParseAdd("Soultrap/0.3.1");
        using var response=await http.SendAsync(req,ct);
        if(response.StatusCode==System.Net.HttpStatusCode.NotFound)throw new InvalidOperationException("No published Soultrap release is available.");
        response.EnsureSuccessStatusCode();
        var release=await response.Content.ReadFromJsonAsync<ReleaseInfo>(cancellationToken:ct)??throw new InvalidDataException("Empty release.");
        ValidateRelease(release);return release;
    }
    public static Version ParseVersion(string tag)
    {
        if(tag is null || !System.Text.RegularExpressions.Regex.IsMatch(tag,"^[vV]?[0-9]+\\.[0-9]+\\.[0-9]+(?:\\.[0-9]+)?$") || !Version.TryParse(tag.TrimStart('v','V'),out var value))
            throw new InvalidDataException("Unsupported stable release version.");
        return value;
    }
    private static Version Normalize(Version v)=>new(v.Major,v.Minor,Math.Max(0,v.Build),Math.Max(0,v.Revision));
    public static bool IsNewer(string tag,Version current)=>Normalize(ParseVersion(tag))>Normalize(current);
    public static void ValidateRelease(ReleaseInfo release)
    {
        ParseVersion(release.Tag);
        if(release.Draft || release.Prerelease || release.Assets is null || release.Url!="https://github.com/zaanw/soultrap/releases/tag/"+release.Tag)
            throw new InvalidDataException("Update must be a published stable release of zaanw/soultrap.");
    }
    private static Uri ValidateAsset(ReleaseAsset asset)
    {
        if(!Uri.TryCreate(asset.Url,UriKind.Absolute,out var uri) || uri.Scheme!="https" || uri.Host!="github.com" || uri.Port!=443 || uri.UserInfo.Length!=0 || uri.Query.Length!=0 || uri.Fragment.Length!=0 || !uri.AbsolutePath.StartsWith("/zaanw/soultrap/releases/download/",StringComparison.Ordinal))
            throw new InvalidDataException("Unexpected release URL.");
        if(asset.Digest is null || !System.Text.RegularExpressions.Regex.IsMatch(asset.Digest,"^sha256:[a-fA-F0-9]{64}$"))
            throw new InvalidDataException("Release has no SHA-256 digest. Download refused.");
        return uri;
    }
    public static ReleaseAsset SelectInstaller(ReleaseInfo release)
    {
        ValidateRelease(release);
        string name=$"Soultrap-{ParseVersion(release.Tag)}-Setup-x64.exe";
        var matches=release.Assets.Where(a=>a.Name==name).ToArray();
        if(matches.Length!=1)throw new InvalidDataException("Release must contain exactly one version-matched Windows x64 installer.");
        var asset=matches[0];var uri=ValidateAsset(asset);
        if(uri.AbsolutePath!=$"/zaanw/soultrap/releases/download/{release.Tag}/{name}")throw new InvalidDataException("Installer asset belongs to another release.");
        return asset;
    }
    public async Task DownloadAsync(ReleaseAsset asset,string destination,CancellationToken ct=default,IProgress<DownloadProgress>? progress=null)
    {
        var uri=ValidateAsset(asset);
        using var response=await http.GetAsync(uri,HttpCompletionOption.ResponseHeadersRead,ct);response.EnsureSuccessStatusCode();
        if(response.RequestMessage?.RequestUri is {} final && (final.Scheme!="https" || !new[]{"github.com","release-assets.githubusercontent.com","objects.githubusercontent.com","github-releases.githubusercontent.com"}.Contains(final.Host)))
            throw new InvalidDataException("Unexpected download redirect destination.");
        long? total=response.Content.Headers.ContentLength;
        if(total>300_000_000)throw new InvalidDataException("Update too large.");
        var temp=destination+"."+Guid.NewGuid().ToString("N")+".download";
        try
        {
            await using(var input=await response.Content.ReadAsStreamAsync(ct))
            await using(var output=new FileStream(temp,FileMode.CreateNew))
            {
                byte[] buffer=new byte[81920];long received=0;int count;
                while((count=await input.ReadAsync(buffer,ct))>0)
                {
                    received+=count;if(received>300_000_000)throw new InvalidDataException("Update too large.");
                    await output.WriteAsync(buffer.AsMemory(0,count),ct);progress?.Report(new(received,total,"Downloading installer"));
                }
                if(total.HasValue && received!=total.Value)throw new InvalidDataException("Incomplete update download.");
                progress?.Report(new(received,received,"Verifying SHA-256"));
            }
            await using(var file=File.OpenRead(temp))await VerifyHashAsync(file,asset,ct);
            File.Move(temp,destination,true);
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public async Task<PreparedInstaller> PrepareInstallerAsync(ReleaseInfo release,Version current,string downloadRoot,IProgress<DownloadProgress>? progress=null,CancellationToken ct=default)
    {
        if(!IsNewer(release.Tag,current))throw new InvalidOperationException("This release is not newer than the installed version.");
        var asset=SelectInstaller(release);
        string relative=$"{ParseVersion(release.Tag)}/{asset.Name}";
        string path=SafePaths.Resolve(downloadRoot,relative);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        await DownloadAsync(asset,path,ct,progress);
        if(OperatingSystem.IsWindows())
            await File.WriteAllTextAsync(path+":Zone.Identifier","[ZoneTransfer]\r\nZoneId=3\r\nHostUrl="+asset.Url+"\r\nReferrerUrl="+release.Url+"\r\n",ct);
        var prepared=new PreparedInstaller(path,release,asset);
        await using var verified=await OpenVerifiedInstallerAsync(prepared,ct);
        progress?.Report(new(verified.Length,verified.Length,"Installer verified — awaiting confirmation"));return prepared;
    }
    private static async Task VerifyHashAsync(Stream file,ReleaseAsset asset,CancellationToken ct)
    {
        ValidateAsset(asset);file.Position=0;
        var hash=Convert.ToHexString(await SHA256.HashDataAsync(file,ct));
        if(!hash.Equals(asset.Digest![7..],StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Update checksum mismatch.");
        file.Position=0;
    }
    public static async Task<FileStream> OpenVerifiedInstallerAsync(PreparedInstaller prepared,CancellationToken ct=default)
    {
        var expected=SelectInstaller(prepared.Release);
        if(prepared.Asset!=expected || System.IO.Path.GetFileName(prepared.Path)!=expected.Name)throw new InvalidDataException("Installer metadata changed.");
        SafePaths.Resolve(System.IO.Path.GetDirectoryName(prepared.Path)!,expected.Name);
        // Deny writes/deletion through process creation to prevent a replaced cached installer.
        var file=new FileStream(prepared.Path,FileMode.Open,FileAccess.Read,FileShare.Read,81920,FileOptions.Asynchronous);
        try
        {
            await VerifyHashAsync(file,expected,ct);
            var header=new byte[64];await file.ReadExactlyAsync(header,ct);
            int offset=BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(60));
            if(header[0]!='M' || header[1]!='Z' || offset<64 || offset>1_000_000 || offset+24>file.Length)throw new InvalidDataException("Update is not a Windows executable.");
            file.Position=offset;var pe=new byte[24];await file.ReadExactlyAsync(pe,ct);
            ushort machine=BinaryPrimitives.ReadUInt16LittleEndian(pe.AsSpan(4));ushort attributes=BinaryPrimitives.ReadUInt16LittleEndian(pe.AsSpan(22));
            if(!pe.AsSpan(0,4).SequenceEqual("PE\0\0"u8) || machine is not (0x14c or 0x8664) || (attributes&2)==0 || (attributes&0x2000)!=0)
                throw new InvalidDataException("Update is not a supported Windows installer executable.");
            file.Position=0;return file;
        }
        catch{await file.DisposeAsync();throw;}
    }
    public static async Task<int> InstallAsync(PreparedInstaller prepared,string installationDirectory,bool userConfirmed,CancellationToken ct=default)
    {
        if(!userConfirmed)throw new InvalidOperationException("Installer launch requires explicit user confirmation.");
        if(!OperatingSystem.IsWindows())throw new PlatformNotSupportedException("Update installation requires Windows.");
        string target=System.IO.Path.GetFullPath(installationDirectory);
        // Fail before closing the application if this installation cannot be updated as the current user.
        string probe=System.IO.Path.Combine(target,".soultrap-update-"+Guid.NewGuid().ToString("N"));
        using(var writable=new FileStream(probe,FileMode.CreateNew,FileAccess.Write,FileShare.None,1,FileOptions.DeleteOnClose)) { }
        await using var locked=await OpenVerifiedInstallerAsync(prepared,ct);
        var start=new ProcessStartInfo(prepared.Path){UseShellExecute=true};
        foreach(string argument in new[]{"/SP-","/SILENT","/NORESTART","/CLOSEAPPLICATIONS","/SOULTRAPUPDATE=1","/DIR="+System.IO.Path.TrimEndingDirectorySeparator(System.IO.Path.GetFullPath(installationDirectory))})start.ArgumentList.Add(argument);
        using var process=Process.Start(start)??throw new IOException("Windows did not start the update installer.");return process.Id;
    }
}
