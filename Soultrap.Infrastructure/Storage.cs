using System.Text.Json;
using Soultrap.Core;
namespace Soultrap.Infrastructure;

public static class JsonFiles
{
    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    public static async Task<T> ReadAsync<T>(string path)
    {
        if (new FileInfo(path).Length > 2 * 1024 * 1024) throw new InvalidDataException("JSON file exceeds 2 MiB.");
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, Options) ?? throw new InvalidDataException("Empty JSON document.");
    }
    public static async Task WriteAsync<T>(string path, T value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            await using (var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            { await JsonSerializer.SerializeAsync(stream, value, Options); await stream.FlushAsync(); }
            File.Move(temp, path, true);
        }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
}
public sealed class AppStorage(string root)
{
    public string Root { get; } = Path.GetFullPath(root);
    public string SettingsFile => Path.Combine(Root, "settings.json");
    public string PresetsDirectory => Path.Combine(Root, "presets");
    public string BackupsDirectory => Path.Combine(Root, "backups");
    public async Task<Settings> LoadSettingsAsync()
    {
        if (!File.Exists(SettingsFile)) return new Settings();
        var settings = await JsonFiles.ReadAsync<Settings>(SettingsFile);
        settings.Validate(); return settings;
    }
    public Task SaveSettingsAsync(Settings settings) { settings.Validate(); return JsonFiles.WriteAsync(SettingsFile, settings); }
    public async Task<string> SavePresetAsync(Preset preset, string? id = null)
    {
        preset.Validate();
        id ??= Guid.NewGuid().ToString("N");
        if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidDataException("Invalid preset id.");
        await JsonFiles.WriteAsync(Path.Combine(PresetsDirectory, id + ".json"), preset); return id;
    }
    public async Task<List<(string Id, Preset Value)>> ListPresetsAsync()
    {
        Directory.CreateDirectory(PresetsDirectory);
        var list = new List<(string, Preset)>();
        foreach (var path in Directory.EnumerateFiles(PresetsDirectory, "*.json"))
        { var p = await JsonFiles.ReadAsync<Preset>(path); p.Validate(); list.Add((Path.GetFileNameWithoutExtension(path), p)); }
        return list;
    }
    public void DeletePreset(string id)
    {
        if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidDataException("Invalid preset id.");
        File.Delete(Path.Combine(PresetsDirectory, id + ".json"));
    }
}
public sealed class EventLog(string root)
{
    private readonly object gate = new();
    public string PathName => Path.Combine(root, "logs", "soultrap.jsonl");
    public void Write(string level, string operation, string message)
    {
        lock (gate)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PathName)!);
            if (File.Exists(PathName) && new FileInfo(PathName).Length > 5_000_000) File.Move(PathName, PathName + ".previous", true);
            File.AppendAllText(PathName, JsonSerializer.Serialize(new { timestamp = DateTimeOffset.UtcNow, level, operation, message }) + Environment.NewLine);
        }
    }
}
