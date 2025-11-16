using System.IO;
using Android.Graphics;
using Avalonia.Media.Imaging;

namespace AvaloniaHello.Android;

internal static class AndroidBitmapExtensions
{
    public static Bitmap? ToAvaloniaBitmap(this Bitmap androidBitmap)
    {
        if (androidBitmap is null)
        {
            return null;
        }

        using var memory = new MemoryStream();
        if (!androidBitmap.Compress(Bitmap.CompressFormat.Png, 100, memory))
        {
            return null;
        }

        memory.Position = 0;
        return new Bitmap(memory);
    }
}
