using System.Text.Json;
using Soultrap.Core;
using Soultrap.Infrastructure;
using Xunit;
namespace Soultrap.Tests;
public sealed class LauncherSettingsTests : IDisposable
{
    private readonly string root=Path.Combine(Path.GetTempPath(),"Soultrap-settings-"+Guid.NewGuid().ToString("N"));
    public LauncherSettingsTests()=>Directory.CreateDirectory(root);
    public void Dispose()=>Directory.Delete(root,true);
    [Fact] public async Task NewPreferencesRoundTrip()
    {
        var settings=new Settings{ShowSplashScreen=false,SplashStyle="Classic",SplashTitle="My Soultrap",ConfirmLaunches=false,AfterLaunch="Minimize",AlwaysOnTop=true,SoftwareRendering=true,UiScale=1.2,SidebarWidth=240,EditorFontSize=17,EditorWordWrap=true,TrackRobloxProcess=false,ProcessPriority="BelowNormal",LogRetentionDays=30};
        var storage=new AppStorage(root);await storage.SaveSettingsAsync(settings);var actual=await storage.LoadSettingsAsync();
        Assert.Equal(JsonSerializer.Serialize(settings),JsonSerializer.Serialize(actual));
    }
    [Fact] public void ExistingSettingsRetainTheirThemeAndGetNewDefaults()
    {
        var old=JsonSerializer.Deserialize<Settings>("{\"Theme\":\"Aero Blue\",\"Opacity\":0.9}")!;old.Validate();
        Assert.Equal("Aero Blue",old.Theme);Assert.True(old.ShowSplashScreen);Assert.Equal("Normal",old.ProcessPriority);
    }
    [Theory][InlineData(.1)][InlineData(double.NaN)][InlineData(2)]
    public void ScaleMustBeFiniteAndInRange(double value)=>Assert.Throws<InvalidDataException>(()=>new Settings{UiScale=value}.Validate());
    [Fact] public void InvalidStartupStyleRejected()=>Assert.Throws<InvalidDataException>(()=>new Settings{SplashStyle="fake"}.Validate());
    [Fact] public void RealtimePriorityRejectedBeforeClientLaunch()=>Assert.Throws<InvalidDataException>(()=>new RobloxService().Launch(new(root,"version-test",DateTime.UtcNow),"Realtime"));
    [Fact] public void UpstreamImportMapsActualSupportedOptions()
    {
        using var input=JsonDocument.Parse("{\"CheckForUpdates\":false,\"ConfirmLaunches\":false,\"WPFSoftwareRender\":true,\"DisableSplashScreen\":true,\"BootstrapperTitle\":\"Imported\",\"CustomFontScale\":1.1,\"EditorFontSize\":16,\"EditorWordWrap\":true,\"EnableActivityTracking\":false,\"CustomAccentColor\":\"#112233\",\"UseDiscordRichPresence\":true}");
        var result=LauncherSettingsImport.Import(input.RootElement,new Settings());
        Assert.Equal(10,result.Imported);Assert.Equal("UseDiscordRichPresence",Assert.Single(result.Unmapped));
        Assert.False(result.Value.ShowSplashScreen);Assert.False(result.Value.CheckUpdates);Assert.True(result.Value.SoftwareRendering);
        Assert.Equal("Imported",result.Value.SplashTitle);Assert.Equal(16,result.Value.EditorFontSize);Assert.Equal("Custom Theme",result.Value.Theme);
    }
    [Fact] public void InvalidImportDoesNotModifyCurrentSettings()
    {
        var current=new Settings();using var input=JsonDocument.Parse("{\"EditorFontSize\":1000}");
        Assert.Throws<InvalidDataException>(()=>LauncherSettingsImport.Import(input.RootElement,current));Assert.Equal(13,current.EditorFontSize);
    }
    [Fact] public void UnsupportedSensitiveOptionsAreReportedWithoutValues()
    {
        using var input=JsonDocument.Parse("{\"RobloxCookie\":\"private-test-value\",\"RiShadeEnabled\":true}");
        var result=LauncherSettingsImport.Import(input.RootElement,new Settings());Assert.Equal(0,result.Imported);Assert.Equal(2,result.Unmapped.Length);Assert.DoesNotContain("private-test-value",string.Join(" ",result.Unmapped));
    }
    [Fact] public async Task CleanerRetainsRecentLogsCurrentLogBackupsAndNonLogs()
    {
        foreach(var name in new[]{"old.log","recent.log","soultrap.jsonl","backup.bin"})File.WriteAllText(Path.Combine(root,name),"test");
        foreach(var name in new[]{"old.log","soultrap.jsonl","backup.bin"})File.SetLastWriteTimeUtc(Path.Combine(root,name),DateTime.UtcNow.AddDays(-60));
        var result=await LogMaintenance.CleanAsync([root],14);Assert.Equal(1,result.Deleted);Assert.Equal(4,result.Bytes);
        Assert.False(File.Exists(Path.Combine(root,"old.log")));Assert.True(File.Exists(Path.Combine(root,"recent.log")));Assert.True(File.Exists(Path.Combine(root,"soultrap.jsonl")));Assert.True(File.Exists(Path.Combine(root,"backup.bin")));
    }
    [Fact] public async Task CleanerDoesNotFollowDirectoryLinks()
    {
        var target=Path.Combine(root,"target");Directory.CreateDirectory(target);var file=Path.Combine(target,"old.log");File.WriteAllText(file,"keep");File.SetLastWriteTimeUtc(file,DateTime.UtcNow.AddDays(-60));
        var link=Path.Combine(root,"link");Directory.CreateSymbolicLink(link,target);
        await Assert.ThrowsAsync<InvalidDataException>(()=>LogMaintenance.CleanAsync([link],14));Assert.True(File.Exists(file));
    }
    [Fact] public async Task CleanerRejectsInvalidRetention()=>await Assert.ThrowsAsync<InvalidDataException>(()=>LogMaintenance.CleanAsync([root],0));
}
