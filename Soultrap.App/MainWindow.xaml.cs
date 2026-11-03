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
        Closed += (_,_)=>tray.Dispose();
        var monitor=new System.Windows.Threading.DispatcherTimer { Interval=TimeSpan.FromSeconds(3) };
        monitor.Tick+=(_,_)=> { if(vm.Config.TrackRobloxProcess)vm.RefreshProcessStatus(); };
        Loaded+=(_,_)=> { Themes.Apply(vm.Config);monitor.Start(); };
        Closed+=(_,_)=>monitor.Stop();
        vm.LaunchCompleted+=OnLaunchCompleted;
        Closed+=(_,_)=>vm.LaunchCompleted-=OnLaunchCompleted;
        PreviewKeyDown+=(_,e)=> { if(e.Key==System.Windows.Input.Key.K && System.Windows.Input.Keyboard.Modifiers==System.Windows.Input.ModifierKeys.Control) { NavigationSearch.Focus();e.Handled=true; } };
    }
    private void MinimizeWindow(object sender,RoutedEventArgs e)=>WindowState=WindowState.Minimized;
    private void MaximizeWindow(object sender,RoutedEventArgs e)=>WindowState=WindowState==WindowState.Maximized?WindowState.Normal:WindowState.Maximized;
    private void CloseWindow(object sender,RoutedEventArgs e)=>Close();
    private void NavigateShortcut(object sender,RoutedEventArgs e)
    {
        string target=((System.Windows.Controls.Button)sender).Tag?.ToString()??"Home";
        NavigationSearch.Clear();Pages.SelectedItem=Pages.Items.Cast<System.Windows.Controls.TabItem>().First(p=>p.Header.ToString()==target);
    }
    public System.Windows.Controls.TabControl NavigationPages=>Pages;
    public void NavigateTo(int index) { NavigationSearch.Clear();Pages.SelectedIndex=index; }
    private void OnLaunchCompleted(string behavior)
    {
        if(behavior=="Minimize")WindowState=WindowState.Minimized;
        else if(behavior=="Close")Close();
    }
    private void NavigationSearch_TextChanged(object sender,System.Windows.Controls.TextChangedEventArgs e)
    {
        if(Pages is null)return;
        string search=NavigationSearch.Text.Trim();
        foreach(System.Windows.Controls.TabItem page in Pages.Items)
            page.Visibility=page.Header.ToString()!.Contains(search,StringComparison.OrdinalIgnoreCase)?Visibility.Visible:Visibility.Collapsed;
        if(Pages.SelectedItem is not System.Windows.Controls.TabItem selected || selected.Visibility!=Visibility.Visible)
            Pages.SelectedItem=Pages.Items.OfType<System.Windows.Controls.TabItem>().FirstOrDefault(p=>p.Visibility==Visibility.Visible);
    }
    private void PreviewMenu(object sender,RoutedEventArgs e)=>PreviewContent.Text=$"{vm.ClassicStyle} · Menu preview\nResume | Settings | Leave\nThese controls only explore Soultrap's local preview.";
    private void PreviewBackpack(object sender,RoutedEventArgs e)=>PreviewContent.Text=$"{vm.ClassicStyle} · Backpack preview\n[1] Build  [2] Move  [3] Paint\nNo items are granted to your Roblox account.";
    private void PreviewChat(object sender,RoutedEventArgs e)=>PreviewContent.Text=$"{vm.ClassicStyle} · Chat preview\nWelcome to the local Classic Roblox sandbox. This is not connected to Roblox chat.";
}
