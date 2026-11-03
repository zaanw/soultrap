using System.Runtime.InteropServices;
using System.Windows.Interop;
namespace Soultrap.App;
internal static class WindowChrome
{
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window,int attribute,ref int value,int size);
    public static void Apply(MainWindow window,bool dark)
    {
        var handle=new WindowInteropHelper(window).Handle;
        if(handle==IntPtr.Zero)return;
        int enabled=dark?1:0;
        // Supported Windows 11 caption treatment. Appearance only; no process hooks.
        DwmSetWindowAttribute(handle,20,ref enabled,sizeof(int));
    }
}
