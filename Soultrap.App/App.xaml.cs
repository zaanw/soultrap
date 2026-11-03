using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Soultrap.Infrastructure;
namespace Soultrap.App;
public partial class App : Application
{
    private ServiceProvider? services;
    private string? smokeRoot;
    private readonly System.Text.StringBuilder bindingErrors = new();
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Soultrap");
        bool smoke = e.Args.Contains("--smoke-test");
        if (smoke)
        {
            smokeRoot = Path.Combine(Path.GetTempPath(), "Soultrap-smoke-" + Guid.NewGuid().ToString("N"));
            root = smokeRoot;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, "settings.json"), "{\"CheckUpdates\":false}");
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Error;
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Listeners.Add(
                new System.Diagnostics.TextWriterTraceListener(new StringWriter(bindingErrors)));
        }
        var collection = new ServiceCollection();
        collection.AddSingleton(new AppStorage(root)); collection.AddSingleton(new EventLog(root));
        collection.AddSingleton(new CustomizationEngine(Path.Combine(root,"backups")));
        collection.AddSingleton<RobloxService>(); collection.AddSingleton(new HttpClient { Timeout=TimeSpan.FromSeconds(40) });
        collection.AddSingleton<UpdateService>(); collection.AddSingleton<MainViewModel>(); collection.AddSingleton<MainWindow>();
        services = collection.BuildServiceProvider();
        DispatcherUnhandledException += (_, args) =>
        {
            args.Handled = true;
            if (smoke) { bindingErrors.AppendLine(args.Exception.ToString()); Shutdown(1); }
            else MessageBox.Show(args.Exception.Message,"Soultrap error");
        };
        var window = services.GetRequiredService<MainWindow>(); MainWindow=window;
        if (smoke) window.Loaded += async (_, _) =>
        {
            try
            {
                var vm = services.GetRequiredService<MainViewModel>();
                for (int i = 0; vm.Busy && i < 200; i++) await Task.Delay(25);
                if (vm.Busy) throw new TimeoutException("Startup did not finish.");
                var panel = (System.Windows.Controls.DockPanel)window.Content;
                var tabs = panel.Children.OfType<System.Windows.Controls.TabControl>().Single();
                foreach (var item in tabs.Items)
                {
                    tabs.SelectedItem = item;
                    window.UpdateLayout();
                    await window.Dispatcher.InvokeAsync(() => { }, System.Windows.Threading.DispatcherPriority.ContextIdle);
                }
                if (bindingErrors.Length > 0) throw new InvalidOperationException(bindingErrors.ToString());
                Shutdown(0);
            }
            catch (Exception error) { bindingErrors.AppendLine(error.ToString()); Shutdown(1); }
        };
        window.Show();
    }
    protected override void OnExit(ExitEventArgs e) { services?.Dispose();
        if (smokeRoot is not null)
        {
            if (bindingErrors.Length > 0) File.WriteAllText(Path.Combine(Path.GetTempPath(),"Soultrap-smoke-errors.txt"),bindingErrors.ToString());
            Directory.Delete(smokeRoot, true);
        }
        base.OnExit(e); }
}
