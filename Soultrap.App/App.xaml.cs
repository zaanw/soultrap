using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Extensions.DependencyInjection;
using Soultrap.Core;
using Soultrap.Infrastructure;
namespace Soultrap.App;
public partial class App : Application
{
    private ServiceProvider? services;
    private string? smokeRoot;
    private readonly System.Text.StringBuilder bindingErrors = new();
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        bool smoke = e.Args.Contains("--smoke-test");
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            if (smoke) { bindingErrors.AppendLine(args.Exception.ToString()); Shutdown(1); }
            else MessageBox.Show(args.Exception.Message,"Soultrap error");
        };
        try { await StartAsync(e,smoke); }
        catch (Exception error)
        {
            if(smoke)bindingErrors.AppendLine(error.ToString());
            else MessageBox.Show(error.Message,"Soultrap startup error");
            Shutdown(1);
        }
    }
    private async Task StartAsync(StartupEventArgs e,bool smoke)
    {
        var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Soultrap");
        if(smoke)
        {
            smokeRoot=Path.Combine(Path.GetTempPath(),"Soultrap-smoke-"+Guid.NewGuid().ToString("N"));root=smokeRoot;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root,"settings.json"),System.Text.Json.JsonSerializer.Serialize(new Settings { CheckUpdates=false,SplashStyle=e.Args.Contains("--classic-splash")?"Classic":"Fluent" }));
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level=System.Diagnostics.SourceLevels.Error;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(new System.Diagnostics.TextWriterTraceListener(new StringWriter(bindingErrors)));
        }
        var collection=new ServiceCollection();
        var storage=new AppStorage(root);
        collection.AddSingleton(storage);collection.AddSingleton(new EventLog(root));
        collection.AddSingleton(new CustomizationEngine(Path.Combine(root,"backups")));
        collection.AddSingleton<RobloxService>();collection.AddSingleton(new HttpClient { Timeout=TimeSpan.FromSeconds(40) });
        collection.AddSingleton<UpdateService>();collection.AddSingleton<MainViewModel>();collection.AddSingleton<MainWindow>();
        services=collection.BuildServiceProvider();
        Settings startup=new();
        try { startup=await storage.LoadSettingsAsync(); }
        catch(Exception error)when(error is System.Text.Json.JsonException or InvalidDataException) { /* View model preserves corrupt input and reports it. */ }
        var vm=services.GetRequiredService<MainViewModel>();vm.PrepareStartup(startup);Themes.Apply(startup);
        StartupWindow? splash=null;
        if(startup.ShowSplashScreen && !e.Args.Contains("--no-splash"))
        {
            splash=new StartupWindow(vm);splash.Show();
            await Dispatcher.InvokeAsync(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);
            if(smoke)Capture(splash,"startup-"+startup.SplashStyle.ToLowerInvariant());
        }
        await vm.InitializeAsync(checkUpdates:false);
        var window=services.GetRequiredService<MainWindow>();MainWindow=window;
        if(smoke)window.Loaded+=async (_,_)=>await SmokeAsync(window,splash is not null,!e.Args.Contains("--no-splash"));
        window.Show();splash?.Close();
        ShutdownMode=ShutdownMode.OnMainWindowClose;
        if(!smoke && vm.Config.CheckUpdates)_=vm.CheckUpdatesInBackgroundAsync();
    }
    private async Task SmokeAsync(MainWindow window,bool splashShown,bool expectSplash)
    {
        try
        {
            if(splashShown!=expectSplash)throw new InvalidOperationException("Startup-screen preference was not respected.");
            var vm=services!.GetRequiredService<MainViewModel>();
            if(vm.StartupProgress!=100)throw new InvalidOperationException("Startup did not complete: "+vm.LastError);
            var tabs=window.NavigationPages;
            foreach(TabItem item in tabs.Items)
            {
                tabs.SelectedItem=item;window.UpdateLayout();
                await window.Dispatcher.InvokeAsync(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);
                if(item.Header.ToString() is "Home" or "Settings" or "Upstream Settings")Capture(window,item.Header.ToString()!.Replace(' ','-').ToLowerInvariant());
            }
            foreach(var theme in Catalog.Themes)
            {
                Themes.Apply(vm.Config with { Theme=theme, Animations=false });
                window.UpdateLayout();
                await window.Dispatcher.InvokeAsync(()=>{},System.Windows.Threading.DispatcherPriority.ContextIdle);
                ThemeVisualChecks.Verify(window);
            }
            Themes.Apply(vm.Config with { Animations=false });
            var search=(TextBox)window.FindName("NavigationSearch");search.Text="Maintenance";
            if(tabs.Items.Cast<TabItem>().Count(t=>t.Visibility==Visibility.Visible)!=1)throw new InvalidOperationException("Navigation search did not filter sections.");
            search.Clear();
            window.NavigateTo(3);
            if(tabs.SelectedIndex!=3)throw new InvalidOperationException("Studio navigation failed.");
            if(bindingErrors.Length>0)throw new InvalidOperationException(bindingErrors.ToString());
            Shutdown(0);
        }
        catch(Exception error){bindingErrors.AppendLine(error.ToString());Shutdown(1);}
    }
    private static void Capture(Window window,string name)
    {
        window.UpdateLayout();
        var root=(FrameworkElement)window.Content;
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);
        bitmap.Render(root);
        string directory=Path.GetFullPath("artifacts/screenshots");Directory.CreateDirectory(directory);
        using var output=File.Create(Path.Combine(directory,name+".png"));var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));encoder.Save(output);
    }
    protected override void OnExit(ExitEventArgs e)
    {
        services?.Dispose();
        if(smokeRoot is not null)
        {
            if(bindingErrors.Length>0)File.WriteAllText(Path.Combine(Path.GetTempPath(),"Soultrap-smoke-errors.txt"),bindingErrors.ToString());
            Directory.Delete(smokeRoot,true);
        }
        base.OnExit(e);
    }
}
