using System.Net.Http.Json;
using System.Text.Json.Serialization;
namespace Soultrap.Infrastructure;
public sealed record ReleaseAsset([property:JsonPropertyName("name")]string Name, [property:JsonPropertyName("browser_download_url")]string Url, [property:JsonPropertyName("digest")]string? Digest);
public sealed record ReleaseInfo([property:JsonPropertyName("tag_name")]string Tag, [property:JsonPropertyName("body")]string Notes, [property:JsonPropertyName("html_url")]string Url, [property:JsonPropertyName("assets")]ReleaseAsset[] Assets);
public sealed class UpdateService(HttpClient http)
{
    public async Task<ReleaseInfo> CheckAsync(CancellationToken ct = default)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/zaanw/soultrap/releases/latest");
        req.Headers.UserAgent.ParseAdd("Soultrap/0.2.1");
        using var response = await http.SendAsync(req, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) throw new InvalidOperationException("No published Soultrap release is available.");
        response.EnsureSuccessStatusCode();
        var release = await response.Content.ReadFromJsonAsync<ReleaseInfo>(cancellationToken:ct) ?? throw new InvalidDataException("Empty release.");
        ParseVersion(release.Tag); return release;
    }
    public static Version ParseVersion(string tag) => Version.TryParse(tag.TrimStart('v','V'), out var value) ? value : throw new InvalidDataException("Unsupported release version.");
    public static bool IsNewer(string tag, Version current) => ParseVersion(tag) > current;
    public async Task DownloadAsync(ReleaseAsset asset, string destination, CancellationToken ct=default)
    {
        var uri = new Uri(asset.Url);
        if (uri.Scheme != "https" || uri.Host != "github.com" || !uri.AbsolutePath.StartsWith("/zaanw/soultrap/releases/download/", StringComparison.Ordinal))
            throw new InvalidDataException("Unexpected release URL.");
        if (asset.Digest is null || !System.Text.RegularExpressions.Regex.IsMatch(asset.Digest, "^sha256:[a-fA-F0-9]{64}$"))
            throw new InvalidDataException("Release has no SHA-256 digest. Download refused.");
        using var response = await http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, ct); response.EnsureSuccessStatusCode();
        var temp = destination + "." + Guid.NewGuid().ToString("N") + ".download";
        try
        {
            await using (var input=await response.Content.ReadAsStreamAsync(ct))
            await using (var output=new FileStream(temp, FileMode.CreateNew))
            {
                byte[] buffer = new byte[81920]; long total=0; int count;
                while ((count=await input.ReadAsync(buffer, ct))>0)
                { total+=count; if (total>300_000_000) throw new InvalidDataException("Update too large."); await output.WriteAsync(buffer.AsMemory(0,count), ct); }
            }
            await using var file = File.OpenRead(temp);
            var digest = Convert.ToHexString(await System.Security.Cryptography.SHA256.HashDataAsync(file, ct));
            if (!digest.Equals(asset.Digest[7..], StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Update checksum mismatch.");
            file.Close(); File.Move(temp,destination,true);
            // Never execute a downloaded update. Publisher pinning must be configured before unattended install.
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
