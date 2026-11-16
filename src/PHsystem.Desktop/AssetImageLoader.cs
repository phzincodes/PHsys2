using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PHsystem.Core;

namespace PHsystem.Desktop;

internal static class AssetImageLoader
{
    public static ImageSource? LoadLogo(IAssetProvider? assets)
    {
        var result = assets?.Get("branding/app-icon.png");
        if (result?.State != AssetState.Available) return null;
        try
        {
            using var stream = new MemoryStream(result.Bytes.ToArray(), writable: false);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count != 1 || decoder.Frames[0].PixelWidth > 4096 || decoder.Frames[0].PixelHeight > 4096) return null;
            var frame = decoder.Frames[0];
            frame.Freeze();
            return frame;
        }
        catch (Exception ex) when (ex is IOException or FormatException or NotSupportedException or ArgumentException or InvalidOperationException)
        { return null; }
    }
}
