using System;
using CoreGraphics;
using Foundation;
using PDFKit;
using UIKit;

namespace AvaloniaHello.iOS;

internal sealed class IosPdfDocument : IDisposable
{
    private readonly PDFDocument _document;

    private IosPdfDocument(PDFDocument document)
    {
        _document = document;
    }

    public static IosPdfDocument? TryOpen(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        try
        {
            var url = NSUrl.FromFilename(filePath);
            if (url is null)
            {
                return null;
            }

            var document = new PDFDocument(url);
            if (document == null || document.PageCount == 0)
            {
                return null;
            }

            return new IosPdfDocument(document);
        }
        catch
        {
            return null;
        }
    }

    public int PageCount => (int)_document.PageCount;

    public UIImage RenderPage(int pageIndex, double scale)
    {
        if (_document.PageCount == 0)
        {
            throw new InvalidOperationException("PDF document is empty");
        }

        var index = Math.Clamp(pageIndex, 0, (int)_document.PageCount - 1);
        var page = _document.GetPage(index) ?? throw new InvalidOperationException("Failed to open PDF page");
        var bounds = page.GetBounds(PDFDisplayBox.MediaBox);
        var targetSize = new CGSize(Math.Max(1, bounds.Width * scale), Math.Max(1, bounds.Height * scale));

        UIGraphics.BeginImageContextWithOptions(targetSize, true, 0);
        var context = UIGraphics.GetCurrentContext() ?? throw new InvalidOperationException("Failed to acquire CGContext");

        context.SetFillColor(UIColor.White.CGColor);
        context.FillRect(new CGRect(0, 0, targetSize.Width, targetSize.Height));

        context.SaveState();
        context.TranslateCTM(0, targetSize.Height);
        context.ScaleCTM(1, -1);

        var transform = page.GetDrawingTransform(PDFDisplayBox.MediaBox, new CGRect(0, 0, targetSize.Width, targetSize.Height), 0, true);
        context.ConcatCTM(transform);
        page.Draw(context);
        context.RestoreState();

        var image = UIGraphics.GetImageFromCurrentImageContext() ?? throw new InvalidOperationException("Failed to produce image from PDF page");
        UIGraphics.EndImageContext();

        return image;
    }

    public void Dispose()
    {
        _document?.Dispose();
    }
}
