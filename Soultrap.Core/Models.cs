using System.Text.Json;
namespace Soultrap.Core;

public sealed record ClientInstallation(string Root, string Version, DateTime LastWrite);
public sealed record Compatibility(bool CanApply, string Reason);
public sealed record CursorPreset(string Name, int Year, string Description, string? Asset);
public sealed record Preset
{
    public int Schema { get; init; } = 1;
    public string Name { get; set; } = "Custom";
    public string Cursor { get; set; } = "Modern Roblox";
    public string ClassicPreview { get; set; } = "Modern";
    public Dictionary<string, JsonElement> Flags { get; set; } = [];
    public void Validate()
    {
        if (Schema != 1 || string.IsNullOrWhiteSpace(Name) || Name.Length > 80 || Flags is null)
            throw new InvalidDataException("Unsupported preset schema or invalid name/flags.");
        if (!Catalog.Cursors.Any(c => c.Name == Cursor) || !Catalog.ClassicStyles.Contains(ClassicPreview))
            throw new InvalidDataException("Unknown cursor or classic preview style.");
        FlagPolicy.Validate(Flags);
    }
}
public sealed record Settings
{
    public int Schema { get; init; } = 1;
    public string Language { get; set; } = "English";
    public string Theme { get; set; } = "Void Dark";
    public string CustomAccent { get; set; } = "#267AC4";
    public double Opacity { get; set; } = 1;
    public bool Animations { get; set; } = true;
    public bool Sounds { get; set; }
    public bool CheckUpdates { get; set; } = true;
    public bool AutoStart { get; set; }
    public bool MinimizeToTray { get; set; }
    public bool ShowSplashScreen { get; set; } = true;
    public string SplashStyle { get; set; } = "Fluent";
    public string SplashTitle { get; set; } = "SOULTRAP";
    public bool ConfirmLaunches { get; set; } = true;
    public string AfterLaunch { get; set; } = "Keep open";
    public bool AlwaysOnTop { get; set; }
    public bool SoftwareRendering { get; set; }
    public double UiScale { get; set; } = 1;
    public double SidebarWidth { get; set; } = 210;
    public double EditorFontSize { get; set; } = 13;
    public bool EditorWordWrap { get; set; }
    public bool TrackRobloxProcess { get; set; } = true;
    public string ProcessPriority { get; set; } = "Normal";
    public int LogRetentionDays { get; set; } = 14;
    public string? RobloxPath { get; set; }
    public DateTimeOffset? LastLaunch { get; set; }
    public string LaunchUri { get; set; } = "roblox-player:";
    public Dictionary<string, bool> Experiments { get; set; } = [];
    public void Validate()
    {
        if (Schema != 1 || !Catalog.Themes.Contains(Theme) || !double.IsFinite(Opacity) || Opacity < .75 || Opacity > 1 || Experiments is null)
            throw new InvalidDataException("Invalid configuration (schema, theme or opacity).");
        if (!new[] { "Fluent", "Classic" }.Contains(SplashStyle) || string.IsNullOrWhiteSpace(SplashTitle) || SplashTitle.Length > 60)
            throw new InvalidDataException("Invalid startup screen style/title.");
        if (!new[] { "Keep open", "Minimize", "Close" }.Contains(AfterLaunch) || !new[] { "Normal", "AboveNormal", "BelowNormal" }.Contains(ProcessPriority))
            throw new InvalidDataException("Invalid launch behavior or process priority.");
        if (!double.IsFinite(UiScale) || UiScale is < .85 or > 1.5 || !double.IsFinite(SidebarWidth) || SidebarWidth is < 180 or > 280 || !double.IsFinite(EditorFontSize) || EditorFontSize is < 10 or > 24 || LogRetentionDays is < 1 or > 365)
            throw new InvalidDataException("Scale, sidebar, editor font or log retention is out of range.");
        if (Language != "English") throw new InvalidDataException("Only English interface is currently available.");
        if (!System.Text.RegularExpressions.Regex.IsMatch(CustomAccent, "^#[0-9a-fA-F]{6}$"))
            throw new InvalidDataException("Custom accent must be #RRGGBB.");
        if (!Uri.TryCreate(LaunchUri, UriKind.Absolute, out var uri) || uri.Scheme != "roblox-player" || LaunchUri.Length > 8192)
            throw new InvalidDataException("Launch URI must use roblox-player:.");
    }
}
public static class Catalog
{
    public static readonly string[] Themes = ["Void Dark", "Void Light", "Aero Blue", "Aero Dark", "Classic Silver", "Retro Green", "Angelcore Glass", "Custom Theme"];
    public static readonly string[] ClassicStyles = ["Classic 2013", "Classic 2016", "Classic 2018", "Classic 2019", "Modern"];
    public static readonly CursorPreset[] Cursors = [
        new("Roblox 2013", 2013, "Original rectangular crosshair inspired by early Roblox. Not an official asset.", "2013.png"),
        new("Roblox 2015", 2015, "Original outlined pointer.", "2015.png"),
        new("Roblox 2017", 2017, "Original compact pointer.", "2017.png"),
        new("Roblox 2019", 2019, "Original narrow pointer.", "2019.png"),
        new("Modern Roblox", 2026, "Restore the original cursor through the backup journal.", null),
        new("Custom Cursor", 0, "Your exported PNG. Engine placement is image centred; arbitrary OS hotspots are not supported by Roblox PNG replacement.", "custom.png")];
    public static IEnumerable<Preset> BuiltInPresets() => new[] {2013, 2016, 2018, 2019}.Select(y => new Preset
    { Name = $"Roblox {y}", Cursor = y == 2013 ? "Roblox 2013" : y == 2016 ? "Roblox 2015" : y == 2018 ? "Roblox 2017" : "Roblox 2019", ClassicPreview = $"Classic {y}" })
        .Append(new Preset { Name = "Modern" });
}
public static class FlagPolicy
{
    // No snapshot is treated as Roblox's current server-side allowlist.
    public const string Status = "Unverified: local JSON only. Applying FastFlags is disabled until an authoritative, current allowlist is available.";
    public static void Validate(Dictionary<string, JsonElement> flags)
    {
        if (flags.Count > 256) throw new InvalidDataException("At most 256 flags.");
        foreach (var (key, value) in flags)
        {
            if (!System.Text.RegularExpressions.Regex.IsMatch(key, "^(FFlag|DFFlag|FInt|DFInt|FString|DFString)[A-Za-z0-9_]{1,120}$"))
                throw new InvalidDataException($"Invalid flag name: {key}");
            if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.String or JsonValueKind.Number))
                throw new InvalidDataException($"Invalid scalar for {key}.");
            string text = value.ToString();
            if (text.Length > 2048) throw new InvalidDataException("Flag value too long.");
            if (key.Contains("Flag") && !bool.TryParse(text, out _)) throw new InvalidDataException($"Boolean required for {key}.");
            if (key.Contains("Int") && !int.TryParse(text, out _)) throw new InvalidDataException($"32-bit integer required for {key}.");
        }
    }
}
