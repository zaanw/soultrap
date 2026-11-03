using System.Windows;
using System.Windows.Controls;
using Soultrap.Infrastructure;
namespace Soultrap.App;
public sealed class OwnerWindow : Window
{
    public OwnerWindow(MainViewModel vm)
    {
        Title="SOULTRAP DEVELOPER CONSOLE";Width=730;Height=530;WindowStartupLocation=WindowStartupLocation.CenterOwner;
        var tabs=new TabControl{Margin=new Thickness(12)};Content=tabs;
        Add("Owner Profile",new TextBlock{Text="👑 SOULTRAP OWNER\nAuthenticated Windows account — local Soultrap profile only.\nNo privileges inside Roblox. Every opening requires Windows credentials.\nWindows Hello is not implemented. Passwordless accounts cannot use this console.\nThis local lock is not a security boundary against programs already running as this Windows account.",Margin=new Thickness(15)});
        var experiments=new StackPanel();experiments.Children.Add(new TextBlock{Text="Local Soultrap feature switches. They do not affect Roblox flags."});
        foreach(var name in new[]{"Show experimental cursor studio notes","Enable verbose diagnostic notes"})
        {
            var check=new CheckBox{Content=name,IsChecked=vm.Config.Experiments.GetValueOrDefault(name),Margin=new Thickness(8)};
            check.Checked+=(_,_)=>vm.Config.Experiments[name]=true;check.Unchecked+=(_,_)=>vm.Config.Experiments[name]=false;experiments.Children.Add(check);
        }
        experiments.Children.Add(new TextBlock{Text="Save these switches with Save / Apply Settings in the main window."});Add("Experimental Features",experiments);
        Add("Feature Flags",new TextBlock{Text="Feature switches are managed in Experimental Features. Save in Settings. Roblox FastFlags are never enabled by owner status.",Margin=new Thickness(12)});
        var studio=new StackPanel();studio.Children.Add(new TextBlock{Text="Cursor Studio, Preset Builder and Theme Studio use the same validated editors in the main window. Select an editor to close this console and focus that tab."});
        foreach(var (label,index) in new[]{("Cursor Studio",3),("Preset Builder",5),("Theme Studio",8)})
        {var button=new Button{Content=label};button.Click+=(_,_)=>{Close();if(Application.Current.MainWindow.Content is DockPanel panel && panel.Children.OfType<TabControl>().FirstOrDefault() is {} mainTabs)mainTabs.SelectedIndex=index;};studio.Children.Add(button);}Add("Studios",studio);
        var inspector=new StackPanel();inspector.Children.Add(new TextBlock{Text=vm.ClientDetails+"\n"+vm.Compatibility});Add("Client Inspector",inspector);
        var diagnostics=new StackPanel();var run=new Button{Content="Open Diagnostics"};run.Click+=(_,_)=>{Close();if(Application.Current.MainWindow.Content is DockPanel panel && panel.Children.OfType<TabControl>().FirstOrDefault() is {} mainTabs)mainTabs.SelectedIndex=7;};diagnostics.Children.Add(run);Add("Diagnostics",diagnostics);
        void Add(string header,UIElement element)=>tabs.Items.Add(new TabItem{Header=header,Content=element});
    }
}
