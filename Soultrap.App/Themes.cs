using System.Windows.Media;
using System.Windows.Media.Animation;
using Soultrap.Core;
namespace Soultrap.App;
public static class Themes
{
    public static void Apply(Settings settings)
    {
        var (accent,surface,ink)=settings.Theme switch
        {
            "Void Dark"=>("#A99CFF","#19191D","#EEEEF2"),
            "Void Light"=>("#6850BC","#F6F6F9","#24232C"),
            "Aero Dark"=>("#79BDFA","#202D3B","#E9F5FF"),
            "Classic Silver"=>("#556677","#E5E7EB","#20272D"),
            "Retro Green"=>("#247847","#E9F4E5","#17351F"),
            "Angelcore Glass"=>("#A2679E","#FFF2FA","#563858"),
            "Custom Theme"=>(settings.CustomAccent,"#F0F4F8","#182B3D"),
            _=>("#267AC4","#EDF5FC","#19364C")
        };
        Change("Accent",accent,settings.Animations);Change("Surface",surface,settings.Animations);Change("Ink",ink,settings.Animations);
        bool dark = settings.Theme is "Void Dark" or "Aero Dark";
        Change("Panel",dark?"#222227":"#FFFFFF",settings.Animations);
        Change("Field",dark?"#2C2C32":"#F0F0F5",settings.Animations);
        Change("Hover",dark?"#36363E":"#E5E3F0",settings.Animations);
        Change("Line",dark?"#3C3C45":"#D9D7E2",settings.Animations);
        Change("Muted",dark?"#A3A3B2":"#6B687A",settings.Animations);
        System.Windows.Media.RenderOptions.ProcessRenderMode = settings.SoftwareRendering ? System.Windows.Interop.RenderMode.SoftwareOnly : System.Windows.Interop.RenderMode.Default;
        if(Application.Current.MainWindow is MainWindow window)
        {
            window.Opacity=settings.Opacity;
            window.Topmost=settings.AlwaysOnTop;
            ((System.Windows.FrameworkElement)window.Content).LayoutTransform = new ScaleTransform(settings.UiScale,settings.UiScale);
        }
    }
    private static void Change(string key,string text,bool animate)
    {
        var color=(Color)ColorConverter.ConvertFromString(text);
        var previous=Application.Current.TryFindResource(key) is SolidColorBrush old ? old.Color : color;
        var brush=new SolidColorBrush(color);
        // A resource consumer may freeze a nonanimated brush immediately on publication.
        // Attach the animation while the fresh brush is still privately owned.
        if(animate && previous!=color)brush.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(previous,color,TimeSpan.FromMilliseconds(180)));
        Application.Current.Resources[key]=brush;
    }
}
