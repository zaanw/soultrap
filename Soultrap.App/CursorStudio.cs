using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Soultrap.Infrastructure;
namespace Soultrap.App;
public static class CursorStudio
{
    public static BitmapSource Load(byte[] bytes)
    {
        SafePaths.ValidatePng(bytes);
        using var input=new MemoryStream(bytes);
        var decoder=new PngBitmapDecoder(input,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
        var image=decoder.Frames[0];image.Freeze();return image;
    }
    public static byte[] Render(byte[] source,int size,int x,int y)
    {
        var image=Load(source);
        if(size is <8 or >128 || x<0 || x>=image.PixelWidth || y<0 || y>=image.PixelHeight) throw new InvalidDataException("Size 8–128; click point must be within the source image.");
        // A double-size transparent canvas anchors the selected click point at its centre without clipping.
        double scale=(double)size/Math.Max(image.PixelWidth,image.PixelHeight);
        var visual=new DrawingVisual();
        using(var dc=visual.RenderOpen()) dc.DrawImage(image,new Rect(size-x*scale,size-y*scale,image.PixelWidth*scale,image.PixelHeight*scale));
        var bitmap=new RenderTargetBitmap(size*2,size*2,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);bitmap.Freeze();
        return Encode(bitmap);
    }
    public static byte[] Encode(BitmapSource bitmap)
    {
        var encoder=new PngBitmapEncoder();encoder.Frames.Add(BitmapFrame.Create(bitmap));using var output=new MemoryStream();encoder.Save(output);return output.ToArray();
    }
}
