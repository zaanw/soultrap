using System.Text.Json;
using Soultrap.Core;
namespace Soultrap.Infrastructure;
public sealed record SettingsImport(Settings Value, int Imported, string[] Unmapped);
public static class LauncherSettingsImport
{
    public static SettingsImport Import(JsonElement document, Settings current)
    {
        if (document.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Expected an upstream settings object.");
        var result = current with { }; int imported = 0; var unmapped = new List<string>();
        foreach (var field in document.EnumerateObject())
        {
            switch (field.Name)
            {
                case "CheckForUpdates": result.CheckUpdates = field.Value.GetBoolean(); break;
                case "ConfirmLaunches": result.ConfirmLaunches = field.Value.GetBoolean(); break;
                case "WPFSoftwareRender": result.SoftwareRendering = field.Value.GetBoolean(); break;
                case "DisableSplashScreen": result.ShowSplashScreen = !field.Value.GetBoolean(); break;
                case "BootstrapperTitle": result.SplashTitle = field.Value.GetString() ?? "SOULTRAP"; break;
                case "CustomFontScale": result.UiScale = field.Value.GetDouble(); break;
                case "EditorFontSize": result.EditorFontSize = field.Value.GetDouble(); break;
                case "EditorWordWrap": result.EditorWordWrap = field.Value.GetBoolean(); break;
                case "EnableActivityTracking": result.TrackRobloxProcess = field.Value.GetBoolean(); break;
                case "CustomAccentColor":
                    if (field.Value.ValueKind == JsonValueKind.Null) { unmapped.Add(field.Name); continue; }
                    result.CustomAccent = field.Value.GetString()!; result.Theme = "Custom Theme"; break;
                default: unmapped.Add(field.Name); continue;
            }
            imported++;
        }
        result.Validate();
        return new(result, imported, unmapped.ToArray());
    }
}
public sealed record UpstreamSetting(string Project, string Name, string Type, string Status, string Destination);
