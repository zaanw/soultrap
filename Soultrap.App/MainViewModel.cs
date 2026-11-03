using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Soultrap.Core;
using Soultrap.Infrastructure;
namespace Soultrap.App;
public sealed class MainViewModel : Observable
{
    private readonly AppStorage storage;
    private readonly EventLog log;
    private readonly CustomizationEngine engine;
    private readonly RobloxService roblox;
    private readonly UpdateService updates;
    private ReleaseInfo? release;
    private ImportedMod? mod;
    private byte[]? sourceCursor, renderedCursor;
    private List<(string Id,Preset Value)> userPresets=[];
    public MainViewModel(AppStorage storage,EventLog log,CustomizationEngine engine,RobloxService roblox,UpdateService updates)
    {
        this.storage=storage;this.log=log;this.engine=engine;this.roblox=roblox;this.updates=updates;
        Action=new AsyncCommand(ExecuteAsync,()=>!Busy);
        selectedCursor=Catalog.Cursors[0];presetJson=Serialize(Catalog.BuiltInPresets().First());
    }
    public event Action<string>? LaunchCompleted;
    public string BrandIcon => Path.Combine(AppContext.BaseDirectory,"Assets","Icons","soultrap.png");
    public string[] SplashStyles => ["Fluent", "Classic"];
    public string[] AfterLaunchOptions => ["Keep open", "Minimize", "Close"];
    public string[] ProcessPriorities => ["Normal", "AboveNormal", "BelowNormal"];
    private string startupStatus = "Loading preferences";
    public string StartupStatus { get => startupStatus; private set => Set(ref startupStatus,value); }
    private double startupProgress;
    public double StartupProgress { get => startupProgress; private set => Set(ref startupProgress,value); }
    public string RobloxStatus => Config.TrackRobloxProcess ? (roblox.IsRunning ? "Roblox is running" : "Roblox is stopped") : "Process monitoring disabled";
    public void RefreshProcessStatus() { Changed(nameof(RobloxStatus)); Changed(nameof(ClientDetails)); }
    private List<UpstreamSetting> upstreamSettings = [];
    public string[] UpstreamProjects => ["All projects", "Bloxstrap", "Fishstrap", "Voidstrap"];
    private string upstreamSearch = "", upstreamProject = "All projects";
    public string UpstreamSearch { get => upstreamSearch; set { Set(ref upstreamSearch,value); Changed(nameof(VisibleUpstreamSettings)); Changed(nameof(UpstreamSummary)); } }
    public string UpstreamProject { get => upstreamProject; set { Set(ref upstreamProject,value); Changed(nameof(VisibleUpstreamSettings)); Changed(nameof(UpstreamSummary)); } }
    public IEnumerable<UpstreamSetting> VisibleUpstreamSettings => upstreamSettings.Where(p => (UpstreamProject == "All projects" || p.Project == UpstreamProject) && (p.Name.Contains(UpstreamSearch,StringComparison.OrdinalIgnoreCase) || p.Destination.Contains(UpstreamSearch,StringComparison.OrdinalIgnoreCase)));
    public string UpstreamSummary => $"{VisibleUpstreamSettings.Count()} of {upstreamSettings.Count} upstream properties · adapted options have working Soultrap behavior.";
    public void PrepareStartup(Settings value) => Config=value;
    public AsyncCommand Action {get;}
    private bool busy;
    public bool Busy {get=>busy;private set{Set(ref busy,value);Changed(nameof(Idle));Action.Refresh();}}
    public bool Idle=>!Busy;
    private string status="Ready",lastError="";
    public string Status {get=>status;private set=>Set(ref status,value);}
    public string LastError {get=>lastError;private set=>Set(ref lastError,value);}
    private Settings config=new();
    public Settings Config {get=>config;private set{Set(ref config,value);Changed(nameof(AutoStart));Changed(nameof(MinimizeToTray));}}
    public bool AutoStart {get=>Config.AutoStart;set{Config.AutoStart=value;Changed();}}
    public bool MinimizeToTray {get=>Config.MinimizeToTray;set{Config.MinimizeToTray=value;Changed();}}
    public string[] Themes=>Catalog.Themes;
    public string[] ClassicStyles=>Catalog.ClassicStyles;
    private string classicStyle="Classic 2013";
    public string ClassicStyle {get=>classicStyle;set{Set(ref classicStyle,value);Changed(nameof(ClassicBackground));Changed(nameof(ClassicPreviewCaption));}}
    public string ClassicBackground=>ClassicStyle switch {"Classic 2013"=>"#AFC7B4","Classic 2016"=>"#B6C4D0","Classic 2018"=>"#B5C9D9","Classic 2019"=>"#C6CBD2",_=>"#DDE5EE"};
    public string ClassicPreviewCaption=>ClassicStyle+" · local interactive study (not a Roblox CoreGui replacement)";
    public ObservableCollection<ClientInstallation> Clients {get;}=[];
    private ClientInstallation? selectedClient;
    public ClientInstallation? SelectedClient {get=>selectedClient;set{Set(ref selectedClient,value);Changed(nameof(ClientDetails));Changed(nameof(Compatibility));}}
    public string ClientDetails=>SelectedClient is {} c?$"Status: {(Config.TrackRobloxProcess ? (roblox.IsRunning?"running":"stopped") : "not monitored")}\nVersion: {c.Version}\nPath: {c.Root}\nLast launch requested: {Config.LastLaunch?.ToLocalTime().ToString("g")??"never"}\nClient updates: managed by official Roblox installer; online version comparison unavailable.":"Roblox not detected. Install the official Win32 client or choose its version directory in Settings. Store/MSIX clients are not supported.";
    public string Compatibility
    {
        get {try{return SelectedClient is {} c?CustomizationEngine.Check(c).Reason:"No client selected.";}catch(Exception e){return e.Message;}}
    }
    public CursorPreset[] CursorPresets=>Catalog.Cursors;
    private CursorPreset selectedCursor;
    public CursorPreset SelectedCursor {get=>selectedCursor;set{Set(ref selectedCursor,value);Changed(nameof(CursorDescription));RefreshCursorPreview();}}
    public string CursorDescription=>$"{SelectedCursor.Name} · {(SelectedCursor.Year==0?"custom":SelectedCursor.Year)}\n{SelectedCursor.Description}{(Config.Experiments.GetValueOrDefault("Show experimental cursor studio notes")?"\nStudio note: Roblox PNG replacement has no .cur hotspot channel; centre the click point before export.":"")}\nFile compatibility: {Compatibility}";
    private ImageSource? cursorPreview;
    public ImageSource? CursorPreview {get=>cursorPreview;private set=>Set(ref cursorPreview,value);}
    public int CursorSize {get;set;}=32;
    public int HotspotX {get;set;}=16;
    public int HotspotY {get;set;}=16;
    private string flagsJson="{}",flagResults="",flagSearch="",flagCategory="All";
    public string FlagsJson {get=>flagsJson;set=>Set(ref flagsJson,value);}
    public string FlagResults {get=>flagResults;private set=>Set(ref flagResults,value);}
    public string FlagSearch {get=>flagSearch;set=>Set(ref flagSearch,value);}
    public string[] FlagCategories=>["All","Boolean","Integer","String"];
    public string FlagCategory {get=>flagCategory;set=>Set(ref flagCategory,value);}
    public string FlagStatus=>FlagPolicy.Status;
    public ObservableCollection<string> PresetNames {get;}=[];
    private string? selectedPresetName;
    public string? SelectedPresetName {get=>selectedPresetName;set=>Set(ref selectedPresetName,value);}
    private string presetJson;
    public string PresetJson {get=>presetJson;set=>Set(ref presetJson,value);}
    private string modDetails="No package imported.",diagnosticText="",updateText="Not checked. No release is assumed to exist.";
    public string ModDetails {get=>modDetails;private set=>Set(ref modDetails,value);}
    public string DiagnosticText {get=>diagnosticText;private set=>Set(ref diagnosticText,value);}
    public string UpdateText {get=>updateText;private set=>Set(ref updateText,value);}
    public ObservableCollection<BackupJournal> Backups {get;}=[];
    public BackupJournal? SelectedBackup {get;set;}
    private static string Serialize<T>(T value)=>JsonSerializer.Serialize(value,JsonFiles.Options);
    private static T Parse<T>(string json)=>JsonSerializer.Deserialize<T>(json,JsonFiles.Options)??throw new InvalidDataException("Empty JSON.");
    private ClientInstallation Client()=>SelectedClient??throw new InvalidOperationException("Select an installed Roblox client first.");
    private string Asset(string file)=>Path.Combine(AppContext.BaseDirectory,"Assets","Cursors",file);
    private static string? Open(string filter)
    {var dialog=new OpenFileDialog{Filter=filter};return dialog.ShowDialog()==true?dialog.FileName:null;}
    private static string? Save(string filter,string name)
    {var dialog=new SaveFileDialog{Filter=filter,FileName=name};return dialog.ShowDialog()==true?dialog.FileName:null;}
    private static void Folder(string path)
    {if(!Directory.Exists(path))throw new DirectoryNotFoundException(path);System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path){UseShellExecute=true});}
    public async Task InitializeAsync(bool checkUpdates = true)
    {
        await Guard("Initialize",async()=>
        {
            try{Config=await storage.LoadSettingsAsync();}
            catch(Exception e)when(e is JsonException or InvalidDataException)
            {File.Copy(storage.SettingsFile,storage.SettingsFile+".corrupt."+Guid.NewGuid().ToString("N"));LastError="Configuration is corrupt. Original preserved at settings.json and a dated .corrupt copy; defaults loaded for this session. "+e.Message;log.Write("warning","config",LastError);}
            StartupProgress=20; StartupStatus="Applying appearance"; ThemesApply();
            StartupProgress=35; StartupStatus="Finding Roblox installations"; await RefreshAsync();
            StartupProgress=60; StartupStatus="Loading presets"; await ReloadPresetsAsync();
            StartupProgress=75; StartupStatus="Reading backup journals"; await RefreshBackupsAsync();
            StartupProgress=90; StartupStatus="Loading compatibility catalog";
            upstreamSettings=await JsonFiles.ReadAsync<List<UpstreamSetting>>(Path.Combine(AppContext.BaseDirectory,"Assets","UpstreamSettings.json"));
            Changed(nameof(VisibleUpstreamSettings)); Changed(nameof(UpstreamSummary));
            RefreshCursorPreview(); StartupProgress=100; StartupStatus="Ready";
        });
        if(checkUpdates && Config.CheckUpdates)await CheckUpdatesInBackgroundAsync();
    }
    private static string DescribeRelease(ReleaseInfo value) => $"{value.Tag} · {(UpdateService.IsNewer(value.Tag,new Version(0,2,1))?"New release available":"Up to date")}\n{value.Notes}\nAuthenticity: no Soultrap publisher certificate is pinned; installation is manual.";
    public async Task CheckUpdatesInBackgroundAsync()
    {
        try
        {
            release=await updates.CheckAsync();UpdateText=DescribeRelease(release);
            log.Write("info","background-updates","Release metadata checked.");
        }
        catch(Exception error)
        {
            UpdateText="Automatic update check unavailable: "+error.Message;
            log.Write("warning","background-updates",error.Message);
        }
    }
    private void ThemesApply()=>Soultrap.App.Themes.Apply(Config);
    private async Task RefreshAsync()
    {
        var existing=SelectedClient?.Root;var found=await roblox.DiscoverAsync(Config.RobloxPath);Clients.Clear();foreach(var c in found)Clients.Add(c);
        SelectedClient=Clients.FirstOrDefault(c=>c.Root==existing)??Clients.FirstOrDefault();
        Changed(nameof(ClientDetails));Changed(nameof(Compatibility));Changed(nameof(CursorDescription));
    }
    private async Task RefreshBackupsAsync()
    {var list=await engine.ListAsync();Backups.Clear();foreach(var b in list)Backups.Add(b);SelectedBackup=Backups.FirstOrDefault();Changed(nameof(SelectedBackup));}
    private async Task ReloadPresetsAsync()
    {
        userPresets=await storage.ListPresetsAsync();PresetNames.Clear();foreach(var p in Catalog.BuiltInPresets())PresetNames.Add(p.Name);
        foreach(var (id,p) in userPresets)PresetNames.Add($"{p.Name} [{id}]");SelectedPresetName=PresetNames.FirstOrDefault();
    }
    private (string? Id,Preset Value) GetSelectedPreset()
    {
        var user=userPresets.FirstOrDefault(p=>$"{p.Value.Name} [{p.Id}]"==SelectedPresetName);
        if(user.Id is not null)return(user.Id,user.Value);
        return(null,Catalog.BuiltInPresets().FirstOrDefault(p=>p.Name==SelectedPresetName)??throw new InvalidOperationException("Select a preset."));
    }
    private Preset EditedPreset(){var p=Parse<Preset>(PresetJson);p.Validate();return p;}
    private Dictionary<string,JsonElement> EditedFlags(){var f=Parse<Dictionary<string,JsonElement>>(FlagsJson);FlagPolicy.Validate(f);return f;}
    private void RefreshCursorPreview()
    {
        try
        {
            var path=SelectedCursor.Asset is null?null:SelectedCursor.Asset=="custom.png"?Path.Combine(storage.Root,"custom.png"):Asset(SelectedCursor.Asset);
            CursorPreview=path is not null&&File.Exists(path)?CursorStudio.Load(File.ReadAllBytes(path)):null;
        }catch(Exception e){LastError=e.Message;CursorPreview=null;}
    }
    private async Task ApplyCursorAsync(CursorPreset cursor)
    {
        if(cursor.Asset is null){await RestoreAllAsync();return;}
        var file=cursor.Asset=="custom.png"?Path.Combine(storage.Root,"custom.png"):Asset(cursor.Asset);
        var bytes=await File.ReadAllBytesAsync(file);bytes=CursorStudio.Encode(CursorStudio.Load(bytes));
        await engine.ApplyAsync(Client(),SafePaths.CursorTargets.ToDictionary(t=>t,t=>bytes),cursor.Name);
        await RefreshBackupsAsync();Status="Cursor files applied and hash-verified. Check visual effect in Roblox.";
    }
    private async Task RestoreLatestAsync()
    {
        var client=Client();var latest=(await engine.ListAsync()).FirstOrDefault(b=>!b.Restored&&b.Root==client.Root&&b.Version==client.Version)??throw new InvalidOperationException("No active backup for this client.");
        await engine.RestoreAsync(client,latest);await RefreshBackupsAsync();Status="Previous cursor bytes restored.";
    }
    private async Task RestoreAllAsync()
    {
        var client=Client();var list=(await engine.ListAsync()).Where(b=>!b.Restored&&b.Root==client.Root&&b.Version==client.Version).ToList();
        if(list.Count==0)throw new InvalidOperationException("No Soultrap backups for this client. Original Roblox files cannot be inferred; use the official Roblox installer to repair.");
        foreach(var b in list)await engine.RestoreAsync(client,b);await RefreshBackupsAsync();Status="All recorded cursor changes for this client restored.";
    }
    private async Task Guard(string operation,Func<Task> action)
    {
        if(Busy)return;Busy=true;LastError="";Status=operation+"…";
        try{await action();if(Status==operation+"…")Status=operation+" completed.";log.Write("info",operation,Status);if(Config.Sounds)System.Media.SystemSounds.Asterisk.Play();}
        catch(Exception ex)
        {
            LastError=operation=="LaunchUri"?"Windows could not launch the Roblox URI. Confirm the registered client and URI format.":ex.Message;
            Status=operation+" failed; see error and logs.";log.Write("error",operation,LastError);
        }
        finally{Busy=false;}
    }
    public Task ExecuteAsync(string operation)=>Guard(operation,()=>RunAsync(operation));
    private async Task RunAsync(string op)
    {
        switch(op)
        {
            case "Refresh":await RefreshAsync();break;
            case "Play":
                if(Config.ConfirmLaunches && MessageBox.Show("Launch the selected Roblox client?","Launch Roblox",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) { Status="Cancelled."; break; }
                Config.Validate(); var launchWarning=await Task.Run(()=>roblox.Launch(Client(),Config.ProcessPriority));
                Config.LastLaunch=DateTimeOffset.UtcNow; await storage.SaveSettingsAsync(Config with{LaunchUri="roblox-player:"});
                Changed(nameof(ClientDetails)); Status="Roblox launch requested; process creation succeeded." + (launchWarning is null ? "" : " " + launchWarning);
                LaunchCompleted?.Invoke(Config.AfterLaunch); break;
            case "LaunchUri":Config.Validate();
                if(Config.ConfirmLaunches && MessageBox.Show("Pass this launch URI to the registered Roblox handler?","Launch Roblox",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) { Status="Cancelled."; break; }
                roblox.LaunchUri(Config.LaunchUri);Status="Launch URI passed to Windows. Process priority is only applied to direct launches.";LaunchCompleted?.Invoke(Config.AfterLaunch);break;
            case "RobloxDownload":RobloxService.OpenOfficialDownload();Status="Official Roblox download page opened. Run its installer to install or update the client.";break;
            case "ClientFolder":Folder(Client().Root);break;
            case "ImportUpstream":
                if(Open("Launcher settings JSON|*.json") is {} upstreamFile)
                {
                    var document=await JsonFiles.ReadAsync<JsonElement>(upstreamFile);
                    var imported=LauncherSettingsImport.Import(document,Config);
                    Config=imported.Value; await SaveSettingsAsync();
                    Status=$"Imported {imported.Imported} supported settings; {imported.Unmapped.Length} unmapped properties were not applied.";
                    DiagnosticText="Unmapped upstream properties:\n"+string.Join("\n",imported.Unmapped);
                } else Status="Cancelled."; break;
            case "DesktopShortcut":CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory));break;
            case "StartMenuShortcut":CreateShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Programs));break;
            case "RobloxLogs":Folder(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Roblox","logs"));break;
            case "BackupsFolder":Directory.CreateDirectory(storage.BackupsDirectory);Folder(storage.BackupsDirectory);break;
            case "CleanLogs":
                Config.Validate(); if(roblox.IsRunning)throw new IOException("Close Roblox before cleaning logs.");
                if(MessageBox.Show($"Delete log files older than {Config.LogRetentionDays} days from Soultrap and Roblox log folders?","Clean old logs",MessageBoxButton.YesNo)!=MessageBoxResult.Yes) { Status="Cancelled."; break; }
                var cleaned=await LogMaintenance.CleanAsync([Path.Combine(storage.Root,"logs"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Roblox","logs")],Config.LogRetentionDays);
                Status=$"Deleted {cleaned.Deleted} old log files ({cleaned.Bytes:N0} bytes); skipped {cleaned.Skipped} locked/inaccessible files.";break;
            case "DataFolder":Directory.CreateDirectory(storage.Root);Folder(storage.Root);break;
            case "Logs":log.Write("info","logs","Opening log directory.");Folder(Path.GetDirectoryName(log.PathName)!);break;
            case "ValidateClient":
                var c=Client();await Task.Run(()=>Authenticode.VerifyRoblox(SafePaths.Resolve(c.Root,"RobloxPlayerBeta.exe")));var compatibility=CustomizationEngine.Check(c);if(!compatibility.CanApply)throw new InvalidDataException(compatibility.Reason);Status="Roblox signature and cursor layout validated; in-game effect remains unverified.";break;
            case "Snapshot":await engine.SnapshotAsync(Client());await RefreshBackupsAsync();break;
            case "Backups":await RefreshBackupsAsync();break;
            case "RestoreLatest":await RestoreLatestAsync();break;
            case "RestoreSelected":await engine.RestoreAsync(Client(),SelectedBackup??throw new InvalidOperationException("Select a backup."));await RefreshBackupsAsync();break;
            case "DeleteBackup":engine.Delete(SelectedBackup??throw new InvalidOperationException("Select a restored backup."));await RefreshBackupsAsync();break;
            case "Defaults":if(MessageBox.Show("Restore all Soultrap cursor changes for this exact Roblox version?","Restore defaults",MessageBoxButton.YesNo)==MessageBoxResult.Yes)await RestoreAllAsync();else Status="Cancelled.";break;
            case "ApplyCursor":await ApplyCursorAsync(SelectedCursor);break;
            case "ImportCursor":
                if(Open("PNG image|*.png") is {} cursorFile){var bytes=await File.ReadAllBytesAsync(cursorFile);var image=CursorStudio.Load(bytes);sourceCursor=CursorStudio.Encode(image);renderedCursor=null;HotspotX=image.PixelWidth/2;HotspotY=image.PixelHeight/2;Changed(nameof(HotspotX));Changed(nameof(HotspotY));CursorPreview=image;}else Status="Cancelled.";break;
            case "RenderCursor":RenderCursor();break;
            case "ExportCursor":
                RenderCursor();if(Save("PNG image|*.png","custom-cursor.png") is {} exportCursor)
                {await File.WriteAllBytesAsync(exportCursor,renderedCursor!);await JsonFiles.WriteAsync(exportCursor+".json",new{Schema=1,HotspotX=CursorSize,HotspotY=CursorSize,Note="Roblox PNG replacement uses image centre; this is editor metadata."});Directory.CreateDirectory(storage.Root);await File.WriteAllBytesAsync(Path.Combine(storage.Root,"custom.png"),renderedCursor!);SelectedCursor=Catalog.Cursors.Last();}else Status="Cancelled.";break;
            case "ValidateFlags":
                var flags=EditedFlags();FlagResults=string.Join("\n",flags.Where(p=>(p.Key.Contains(FlagSearch,StringComparison.OrdinalIgnoreCase)||p.Value.ToString().Contains(FlagSearch,StringComparison.OrdinalIgnoreCase))&&(FlagCategory=="All"||FlagCategory=="Boolean"&&p.Key.Contains("Flag")||FlagCategory=="Integer"&&p.Key.Contains("Int")||FlagCategory=="String"&&p.Key.Contains("String"))).Select(p=>$"{p.Key} = {p.Value} · compatibility unverified"));Status=$"{flags.Count} values validated locally; not applied.";break;
            case "ImportFlags":if(Open("JSON|*.json") is {} flagFile){var imported=await JsonFiles.ReadAsync<Dictionary<string,JsonElement>>(flagFile);FlagPolicy.Validate(imported);FlagsJson=Serialize(imported);}else Status="Cancelled.";break;
            case "ExportFlags":var exportFlags=EditedFlags();if(Save("JSON|*.json","ClientAppSettings.json") is {} flagOut)await JsonFiles.WriteAsync(flagOut,exportFlags);else Status="Cancelled.";break;
            case "LoadPreset":PresetJson=Serialize(GetSelectedPreset().Value);break;
            case "NewPreset":SelectedPresetName=PresetNames.FirstOrDefault();PresetJson=Serialize(new Preset());break;
            case "DuplicatePreset":var copy=EditedPreset() with{Name=EditedPreset().Name+" copy"};await storage.SavePresetAsync(copy);await ReloadPresetsAsync();PresetJson=Serialize(copy);break;
            case "SavePreset":var edited=EditedPreset();var id=GetSelectedPreset().Id;await storage.SavePresetAsync(edited,id);await ReloadPresetsAsync();break;
            case "DeletePreset":var deleteId=GetSelectedPreset().Id??throw new InvalidOperationException("Built-in presets cannot be deleted. Duplicate first.");storage.DeletePreset(deleteId);await ReloadPresetsAsync();break;
            case "ImportPreset":if(Open("JSON|*.json") is {} presetFile){var imported=await JsonFiles.ReadAsync<Preset>(presetFile);imported.Validate();await storage.SavePresetAsync(imported);PresetJson=Serialize(imported);await ReloadPresetsAsync();}else Status="Cancelled.";break;
            case "ExportPreset":var exportPreset=EditedPreset();if(Save("JSON|*.json","soultrap-preset.json") is {} presetOut)await JsonFiles.WriteAsync(presetOut,exportPreset);else Status="Cancelled.";break;
            case "ApplyPreset":var p=EditedPreset();if(p.Flags.Count>0)throw new InvalidOperationException(FlagPolicy.Status);await ApplyCursorAsync(Catalog.Cursors.First(c=>c.Name==p.Cursor));ClassicStyle=p.ClassicPreview;Status+=" Classic UI is preview only.";break;
            case "InspectMod":if(Open("Soultrap mod ZIP|*.zip") is {} modFile){mod=await ModPackages.ReadAsync(modFile,Client().Version);foreach(var bytes in mod.Files.Values)CursorStudio.Load(bytes);ModDetails=Serialize(mod.Manifest);Status="Package validated for this client version. Not installed yet.";}else Status="Cancelled.";break;
            case "ApplyMod":if(mod is null)throw new InvalidOperationException("Inspect a package first.");if(mod.Manifest.RequiredVersion!=Client().Version)throw new InvalidOperationException("Client changed; inspect package again.");await engine.ApplyAsync(Client(),mod.Files,mod.Manifest.Name);await RefreshBackupsAsync();Status="Mod files applied and verified. In-game effect unverified.";break;
            case "RemoveMod":mod=null;ModDetails="Package removed from memory. Installed files are retained; use Disable or restore its backup to remove changes.";break;
            case "Diagnostics":await RefreshAsync();DiagnosticText=$"Soultrap 0.2.1\nOS: {Environment.OSVersion}\nRuntime: {Environment.Version}\nData: {storage.Root}\n{ClientDetails}\n{Compatibility}\nFastFlags: {FlagPolicy.Status}\nWindows GUI/Roblox visual effects require manual validation.";if(Config.Experiments.GetValueOrDefault("Enable verbose diagnostic notes"))DiagnosticText+=$"\nVerbose: CPU count={Environment.ProcessorCount}; 64-bit process={Environment.Is64BitProcess}; managed memory={GC.GetTotalMemory(false)} bytes";await RefreshBackupsAsync();break;
            case "SaveSettings":await SaveSettingsAsync();break;
            case "ResetSettings":Config=new Settings();await SaveSettingsAsync();break;
            case "ImportSettings":if(Open("JSON|*.json") is {} settingsFile){var imported=await JsonFiles.ReadAsync<Settings>(settingsFile);imported.Validate();Config=imported;await SaveSettingsAsync();}else Status="Cancelled.";break;
            case "ExportSettings":Config.Validate();if(Save("JSON|*.json","soultrap-settings.json") is {} settingsOut)await JsonFiles.WriteAsync(settingsOut,Config with{LaunchUri="roblox-player:"});else Status="Cancelled.";break;
            case "Updates":release=await updates.CheckAsync();UpdateText=DescribeRelease(release);break;
            case "DownloadUpdate":
                if(release is null)throw new InvalidOperationException("Check releases first.");var asset=release.Assets.FirstOrDefault(a=>a.Name.EndsWith("-win-x64.zip",StringComparison.OrdinalIgnoreCase))??throw new InvalidOperationException("Release has no x64 portable ZIP.");
                if(Save("ZIP|*.zip",Path.GetFileName(asset.Name)) is {} destination){await updates.DownloadAsync(asset,destination);Status="Archive downloaded and SHA-256 verified against GitHub metadata. Publisher authenticity is not pinned; nothing was executed.";}else Status="Cancelled.";break;
            case "Owner":OwnerAuthentication.Authenticate(storage.Root,new WindowInteropHelper(Application.Current.MainWindow).Handle);new OwnerWindow(this){Owner=Application.Current.MainWindow}.ShowDialog();break;
            default:throw new InvalidOperationException("Unknown action.");
        }
    }
    private static void CreateShortcut(string folder)
    {
        var exe=Environment.ProcessPath??throw new InvalidOperationException("Executable location unavailable.");
        if(!Path.GetFileName(exe).Equals("Soultrap.exe",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Shortcuts require the published Soultrap.exe.");
        Directory.CreateDirectory(folder);var path=Path.Combine(folder,"Soultrap.lnk");
        if(File.Exists(path))throw new IOException("A Soultrap shortcut already exists; it was preserved.");
        var type=Type.GetTypeFromProgID("WScript.Shell")??throw new InvalidOperationException("Windows shortcut service unavailable.");
        dynamic shell=Activator.CreateInstance(type)!; object? shortcutObject=null;
        try
        {
            dynamic shortcut=shell.CreateShortcut(path);shortcutObject=shortcut;
            shortcut.TargetPath=exe;shortcut.WorkingDirectory=Path.GetDirectoryName(exe);shortcut.IconLocation=exe+",0";shortcut.Description="Soultrap Roblox customization";shortcut.Save();
        }
        finally
        {
            if(shortcutObject is not null)System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcutObject);
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }
    }
    private void RenderCursor()
    {
        renderedCursor=CursorStudio.Render(sourceCursor??throw new InvalidOperationException("Import a PNG first."),CursorSize,HotspotX,HotspotY);CursorPreview=CursorStudio.Load(renderedCursor);
    }
    private async Task SaveSettingsAsync()
    {
        Config.Validate();
        using var key=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if(Config.AutoStart)
        {
            var path=Environment.ProcessPath??throw new InvalidOperationException("Executable location unavailable.");
            if(!Path.GetFileName(path).Equals("Soultrap.exe",StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Autostart requires the published Soultrap.exe.");
            key.SetValue("Soultrap",$"\"{path}\"");
        }else key.DeleteValue("Soultrap",false);
        await storage.SaveSettingsAsync(Config with{LaunchUri="roblox-player:"});Changed(nameof(Config));Changed(nameof(RobloxStatus));ThemesApply();await RefreshAsync();
    }
}
