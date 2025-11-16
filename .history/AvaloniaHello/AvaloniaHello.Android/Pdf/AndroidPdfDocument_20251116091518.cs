using System;
using Android.Graphics;
using Android.Graphics.Pdf;
using Android.OS;

namespace AvaloniaHello.Android;

internal sealed class AndroidPdfDocument : IDisposable
{
    private const PdfRenderMode RenderModeForDisplay = PdfRenderMode.ForDisplay;
    private ParcelFileDescriptor? _descriptor;
    private PdfRenderer? _renderer;

    private AndroidPdfDocument(ParcelFileDescriptor descriptor, PdfRenderer renderer)
    {
        _descriptor = descriptor;
        _renderer = renderer;
    }

    public static AndroidPdfDocument? TryOpen(string filePath)
    {
        try
        {
            var javaFile = new Java.IO.File(filePath);
            if (!javaFile.Exists())
            {
                return null;
            }

            var descriptor = ParcelFileDescriptor.Open(javaFile, ParcelFileMode.ReadOnly);
            if (descriptor is null)
            {
                return null;
            }

            var renderer = new PdfRenderer(descriptor);
            return new AndroidPdfDocument(descriptor, renderer);
        }
        catch
        {
            return null;
        }
    }

    public int PageCount => _renderer?.PageCount ?? 0;

    public Bitmap RenderPage(int pageIndex, float scale)
    {
        var renderer = _renderer;
        if (renderer is null)
        {
            throw new InvalidOperationException("PDF renderer not initialized");
        }

        if (renderer.PageCount == 0)
        {
            throw new InvalidOperationException("PDF document has zero pages");
        }

        var targetIndex = Math.Clamp(pageIndex, 0, renderer.PageCount - 1);

        using var page = renderer.OpenPage(targetIndex);
        var width = Math.Max(1, (int)(page.Width * scale));
        var height = Math.Max(1, (int)(page.Height * scale));

        var bitmap = Bitmap.CreateBitmap(width, height, Bitmap.Config.Argb8888) ??
                     throw new InvalidOperationException("Failed to allocate bitmap for PDF rendering");

        var destination = new Rect(0, 0, width, height);
        page.Render(bitmap, destination, null, RenderModeForDisplay);

        return bitmap;
    }

    public void Dispose()
    {
        _renderer?.Close();
        _renderer?.Dispose();
        _renderer = null;

        _descriptor?.Close();
        _descriptor?.Dispose();
        _descriptor = null;
    }
}
