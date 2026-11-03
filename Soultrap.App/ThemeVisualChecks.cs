using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Soultrap.App;
internal static class ThemeVisualChecks
{
    public static void Verify(MainWindow window)
    {
        var page=(Border)window.NavigationPages.Template.FindName("PageSurface",window.NavigationPages);
        var error=(Border)window.FindName("ErrorSurface");
        AssertPixel(page,(SolidColorBrush)Application.Current.FindResource("Surface"),"page");
        AssertPixel(error,(SolidColorBrush)Application.Current.FindResource("Panel"),"error bar");
        if(window.NavigationPages.SelectedContent is ScrollViewer viewer)
            AssertPixel(viewer,(SolidColorBrush)Application.Current.FindResource("Surface"),"page scroll viewer");
        var subtitle=(TextBlock)window.FindName("HomeSubtitle");
        var ink=(SolidColorBrush)Application.Current.FindResource("Ink");
        if(subtitle.Foreground is not SolidColorBrush body || body.Color!=ink.Color)
            throw new InvalidOperationException("Page body text is not using the theme foreground.");
        var button=Descendants<Button>(window).First(b=>b.Content?.ToString()=="Save settings");
        var text=Descendants<TextBlock>(button).First(t=>t.Text=="Save settings");
        if(text.Foreground is not SolidColorBrush actual || button.Foreground is not SolidColorBrush expected || actual.Color!=expected.Color)
            throw new InvalidOperationException("Button text does not inherit its contrast foreground.");
    }
    private static void AssertPixel(FrameworkElement element,SolidColorBrush expected,string description)
    {
        var bitmap=new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth),(int)Math.Ceiling(element.ActualHeight),96,96,PixelFormats.Pbgra32);
        bitmap.Render(element);byte[] pixel=new byte[4];bitmap.CopyPixels(new Int32Rect(2,2,1,1),pixel,4,0);
        if(pixel[3]!=255 || Math.Abs(pixel[0]-expected.Color.B)>2 || Math.Abs(pixel[1]-expected.Color.G)>2 || Math.Abs(pixel[2]-expected.Color.R)>2)
            throw new InvalidOperationException($"Rendered {description} does not match the selected theme: BGRA {string.Join(",",pixel)} versus {expected.Color}.");
    }
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T:DependencyObject
    {
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)
        {
            var child=VisualTreeHelper.GetChild(root,i);
            if(child is T typed)yield return typed;
            foreach(var nested in Descendants<T>(child))yield return nested;
        }
    }
}
