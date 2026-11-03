using System.Windows;
namespace Soultrap.App;
public partial class StartupWindow : Window
{
    public StartupWindow(MainViewModel model) { InitializeComponent(); DataContext=model; }
}
