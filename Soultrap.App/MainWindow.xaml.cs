using System.Windows;
namespace Soultrap.App;
public partial class MainWindow : Window
{
    private readonly MainViewModel vm;
    private readonly System.Windows.Forms.NotifyIcon tray;
    public MainWindow(MainViewModel model)
    {
        InitializeComponent(); vm=model; DataContext=vm;
        tray = new System.Windows.Forms.NotifyIcon { Text="Soultrap", Icon=System.Drawing.SystemIcons.Application };
        tray.DoubleClick += (_,_)=>Dispatcher.Invoke(()=>{ Show(); WindowState=WindowState.Normal; Activate(); tray.Visible=false; });
        var menu = new System.Windows.Forms.ContextMenuStrip();
        menu.Items.Add("Open Soultrap",null,(_,_)=>Dispatcher.Invoke(()=>{Show();WindowState=WindowState.Normal; tray.Visible=false;}));
        menu.Items.Add("Exit",null,(_,_)=>Dispatcher.Invoke(Close)); tray.ContextMenuStrip=menu;
        StateChanged += (_,_)=> { if(WindowState==WindowState.Minimized && vm.MinimizeToTray){tray.Visible=true;Hide();} };
        Closed += (_,_)=>tray.Dispose(); Loaded += async (_,_)=>await vm.InitializeAsync();
    }
    private void PreviewMenu(object sender,RoutedEventArgs e)=>PreviewContent.Text=$"{vm.ClassicStyle} · Menu preview\nResume | Settings | Leave\nThese controls only explore Soultrap's local preview.";
    private void PreviewBackpack(object sender,RoutedEventArgs e)=>PreviewContent.Text=$"{vm.ClassicStyle} · Backpack preview\n[1] Build  [2] Move  [3] Paint\nNo items are granted to your Roblox account.";
    private void PreviewChat(object sender,RoutedEventArgs e)=>PreviewContent.Text=$"{vm.ClassicStyle} · Chat preview\nWelcome to the local Classic Roblox sandbox. This is not connected to Roblox chat.";
}
