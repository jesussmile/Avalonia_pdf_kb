using System;
using CoreGraphics;
using Foundation;
using UIKit;

namespace AvaloniaHello.iOS;

internal sealed class IosPdfDocument : IDisposable
{
    private readonly CGPDFDocument _document;

    private IosPdfDocument(CGPDFDocument document)
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

            var document = CGPDFDocument.FromFile(filePath);
            if (document == null || document.Pages == 0)
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

    public int PageCount => (int)_document.Pages;

    public UIImage RenderPage(int pageIndex, double scale)
    {
        if (_document.Pages == 0)
        {
            throw new InvalidOperationException("PDF document is empty");
        }

        var index = Math.Clamp(pageIndex, 0, _document.Pages - 1);
        var page = _document.GetPage(index + 1) ?? throw new InvalidOperationException("Failed to open PDF page");
        var bounds = page.GetBoxRect(CGPDFBox.Media);
        var targetSize = new CGSize(Math.Max(1, bounds.Width * scale), Math.Max(1, bounds.Height * scale));

        var renderer = new UIGraphicsImageRenderer(targetSize);
        var image = renderer.CreateImage(rendererContext =>
        {
            var context = rendererContext.CGContext;
            context.SetFillColor(UIColor.White.CGColor);
            context.FillRect(new CGRect(0, 0, targetSize.Width, targetSize.Height));

            context.SaveState();
            context.TranslateCTM(0, targetSize.Height);
            context.ScaleCTM(1, -1);

            var transform = page.GetDrawingTransform(CGPDFBox.Media, new CGRect(0, 0, targetSize.Width, targetSize.Height), 0, true);
            context.ConcatCTM(transform);
            context.DrawPDFPage(page);
            context.RestoreState();
        });

        return image;
    }

    public void Dispose()
    {
        _document?.Dispose();
    }
}
