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
            "Aero Dark"=>("#79BDFA","#202D3B","#E9F5FF"),
            "Classic Silver"=>("#556677","#E5E7EB","#20272D"),
            "Retro Green"=>("#247847","#E9F4E5","#17351F"),
            "Angelcore Glass"=>("#A2679E","#FFF2FA","#563858"),
            "Custom Theme"=>(settings.CustomAccent,"#F0F4F8","#182B3D"),
            _=>("#267AC4","#EDF5FC","#19364C")
        };
        Change("Accent",accent,settings.Animations);Change("Surface",surface,settings.Animations);Change("Ink",ink,settings.Animations);
        if(Application.Current.MainWindow is {} window)window.Opacity=settings.Opacity;
    }
    private static void Change(string key,string text,bool animate)
    {
        var color=(Color)ColorConverter.ConvertFromString(text);
        var brush=new SolidColorBrush(color);Application.Current.Resources[key]=brush;
        if(animate)brush.BeginAnimation(SolidColorBrush.ColorProperty,new ColorAnimation(Colors.White,color,TimeSpan.FromMilliseconds(180)));
    }
}
