using System;
using Avalonia.Media.Imaging;
using UIKit;

namespace AvaloniaHello.iOS;

internal static class IosBitmapExtensions
{
    public static Bitmap? ToAvaloniaBitmap(this UIImage image)
    {
        if (image is null)
        {
            return null;
        }

        using var data = image.AsPNG();
        if (data is null)
        {
            return null;
        }

        using var stream = data.AsStream();
        return new Bitmap(stream);
    }
}
