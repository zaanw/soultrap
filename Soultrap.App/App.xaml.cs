using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Soultrap.Infrastructure;
namespace Soultrap.App;
public partial class App : Application
{
    private ServiceProvider? services;
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Soultrap");
        var collection = new ServiceCollection();
        collection.AddSingleton(new AppStorage(root)); collection.AddSingleton(new EventLog(root));
        collection.AddSingleton(new CustomizationEngine(Path.Combine(root,"backups")));
        collection.AddSingleton<RobloxService>(); collection.AddSingleton(new HttpClient { Timeout=TimeSpan.FromSeconds(40) });
        collection.AddSingleton<UpdateService>(); collection.AddSingleton<MainViewModel>(); collection.AddSingleton<MainWindow>();
        services = collection.BuildServiceProvider();
        DispatcherUnhandledException += (_, args) => { MessageBox.Show(args.Exception.Message,"Soultrap error"); args.Handled=true; };
        var window = services.GetRequiredService<MainWindow>(); MainWindow=window; window.Show();
    }
    protected override void OnExit(ExitEventArgs e) { services?.Dispose(); base.OnExit(e); }
}
