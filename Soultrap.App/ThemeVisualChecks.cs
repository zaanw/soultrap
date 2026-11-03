using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace Soultrap.App;
internal static class ThemeVisualChecks
{
    public static void Verify(MainWindow window)
    {
        var root=(FrameworkElement)window.Content;
        var frame=new RenderTargetBitmap((int)Math.Ceiling(root.ActualWidth),(int)Math.Ceiling(root.ActualHeight),96,96,PixelFormats.Pbgra32);
        frame.Render(root);
        var page=(Border)window.NavigationPages.Template.FindName("PageSurface",window.NavigationPages);
        var error=(Border)window.FindName("ErrorSurface");
        AssertPixel(root,frame,page,(SolidColorBrush)Application.Current.FindResource("Surface"),"page");
        AssertPixel(root,frame,error,(SolidColorBrush)Application.Current.FindResource("Panel"),"error bar");
        if(window.NavigationPages.SelectedContent is ScrollViewer viewer)
            AssertPixel(root,frame,viewer,(SolidColorBrush)Application.Current.FindResource("Surface"),"page scroll viewer");
        var subtitle=(TextBlock)window.FindName("HomeSubtitle");
        var ink=(SolidColorBrush)Application.Current.FindResource("Ink");
        if(subtitle.Foreground is not SolidColorBrush body || body.Color!=ink.Color)
            throw new InvalidOperationException("Page body text is not using the theme foreground.");
        var button=Descendants<Button>(window).First(b=>b.Content?.ToString()=="Save settings");
        var text=Descendants<TextBlock>(button).First(t=>t.Text=="Save settings");
        if(text.Foreground is not SolidColorBrush actual || button.Foreground is not SolidColorBrush expected || actual.Color!=expected.Color)
            throw new InvalidOperationException("Button text does not inherit its contrast foreground.");
    }
    private static void AssertPixel(FrameworkElement root,BitmapSource frame,FrameworkElement element,SolidColorBrush expected,string description)
    {
        // Render the shared root and translate the sample into that coordinate system.
        // Rendering a nested visual alone retains its layout offset and can sample empty pixels.
        var location=element.TranslatePoint(new Point(2,2),root);
        byte[] pixel=new byte[4];frame.CopyPixels(new Int32Rect((int)Math.Round(location.X),(int)Math.Round(location.Y),1,1),pixel,4,0);
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
