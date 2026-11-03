using System.IO.Compression;
namespace Soultrap.Infrastructure;
public sealed record ModManifest(int Schema, string Name, string RequiredVersion, Dictionary<string, string> Files);
public sealed record ImportedMod(ModManifest Manifest, Dictionary<string, byte[]> Files);
public static class ModPackages
{
    public static async Task<ImportedMod> ReadAsync(string path, string version)
    {
        if (new FileInfo(path).Length > 8_000_000) throw new InvalidDataException("Mod exceeds 8 MB.");
        using var zip = ZipFile.OpenRead(path);
        if (zip.Entries.Count is < 2 or > 4 || zip.Entries.Sum(e=>e.Length) > 6_000_000) throw new InvalidDataException("Mod archive limits exceeded.");
        if (zip.Entries.Select(e=>e.FullName).Distinct(StringComparer.OrdinalIgnoreCase).Count() != zip.Entries.Count) throw new InvalidDataException("Duplicate archive entries.");
        foreach (var entry in zip.Entries)
            if (entry.FullName.Contains('\\') || entry.FullName.Contains(':') || entry.FullName.Split('/').Any(p=>p is ".." or "." or "") || entry.FullName.StartsWith('/') || ((entry.ExternalAttributes >> 16) & 0xF000) == 0xA000)
                throw new InvalidDataException("Unsafe archive entry.");
        var manifestEntry = zip.GetEntry("mod.json") ?? throw new InvalidDataException("mod.json missing.");
        if (manifestEntry.Length > 32768) throw new InvalidDataException("Manifest too large.");
        await using var stream = manifestEntry.Open();
        var manifest = await System.Text.Json.JsonSerializer.DeserializeAsync<ModManifest>(stream, JsonFiles.Options) ?? throw new InvalidDataException("Invalid manifest.");
        if (manifest.Schema != 1 || string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 80 || manifest.Files is null || manifest.Files.Count is < 1 or > 2)
            throw new InvalidDataException("Unsupported manifest.");
        if (manifest.RequiredVersion != version) throw new InvalidDataException("Mod requires a different client version. Recheck it for this installation.");
        var files = new Dictionary<string, byte[]>();
        foreach (var (target, source) in manifest.Files)
        {
            if (!SafePaths.IsCursorTarget(target)) throw new InvalidDataException("Only cursor PNG targets are permitted.");
            var entry = zip.GetEntry(source) ?? throw new InvalidDataException("PNG missing.");
            if (entry.Length > 2_000_000 || !source.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Unsupported mod content.");
            await using var input = entry.Open(); using var output = new MemoryStream(); await input.CopyToAsync(output);
            var bytes = output.ToArray(); SafePaths.ValidatePng(bytes); files.Add(target,bytes);
        }
        if (zip.Entries.Any(e=>e.FullName != "mod.json" && !manifest.Files.Values.Contains(e.FullName))) throw new InvalidDataException("Undeclared package content.");
        return new(manifest, files);
    }
}
