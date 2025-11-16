using System.IO;
using AndroidBitmap = global::Android.Graphics.Bitmap;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace AvaloniaHello.Android;

internal static class AndroidBitmapExtensions
{
    public static AvaloniaBitmap? ToAvaloniaBitmap(this AndroidBitmap androidBitmap)
    {
        if (androidBitmap is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        if (!androidBitmap.Compress(Android.Graphics.Bitmap.CompressFormat.Png, 100, memory))
        {
            return null;
        }

        memory.Position = 0;
        return new AvaloniaBitmap(memory);
    }
}
