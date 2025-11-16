using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MuPDFCore;
using MuPDFCore.MuPDFRenderer;

namespace AvaloniaHello.ViewModels;

public partial class PdfViewerViewModel : ViewModelBase, IDisposable
{
    private readonly string _documentPath;
    private readonly Action? _navigateBack;
    private readonly object _documentLock = new();
    private MuPDFContext? _context;
    private MuPDFDocument? _document;
    private PDFRenderer? _renderer;
    private bool _disposed;

    public PdfViewerViewModel(string documentPath, Action? navigateBack)
    {
        _documentPath = documentPath;
        _navigateBack = navigateBack;
        BackCommand = new RelayCommand(() => _navigateBack?.Invoke());
    }

    public ObservableCollection<PageThumbnail> Thumbnails { get; } = new();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private int _activePageIndex;

    public bool IsInitialized { get; private set; }

    public IRelayCommand BackCommand { get; }

    public int PageCount => _document?.Pages.Length ?? 0;

    public void LoadDocument()
    {
        if (IsInitialized || !System.IO.File.Exists(_documentPath))
        {
            return;
        }

        _context = new MuPDFContext();
        _document = new MuPDFDocument(_context, _documentPath);
        IsInitialized = true;

        _ = GenerateThumbnailsAsync();
    }

    public void AttachRenderer(PDFRenderer renderer)
    {
        _renderer = renderer;

        if (!IsInitialized || _document is null)
        {
            return;
        }

        renderer.ReleaseResources();
        renderer.Initialize(_document, threadCount: 0, pageNumber: ActivePageIndex);
        renderer.Cover();
    }

    private async Task GenerateThumbnailsAsync()
    {
        if (_document is null)
        {
            return;
        }

        IsBusy = true;

        var pageCount = PageCount;

        Dispatcher.UIThread.Post(Thumbnails.Clear);

        await Task.Run(() =>
        {
            for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
            {
                var thumbnail = CreateThumbnail(pageIndex);
                Dispatcher.UIThread.Post(() => Thumbnails.Add(thumbnail));
            }
        });

        if (pageCount > 0)
        {
            ActivePageIndex = 0;
        }

        IsBusy = false;
    }

    private PageThumbnail CreateThumbnail(int pageNumber)
    {
        lock (_documentLock)
        {
            if (_document is null)
            {
                throw new InvalidOperationException("Document not loaded");
            }

            var bounds = _document.Pages[pageNumber].Bounds;
            const double targetWidth = 180d;
            var zoom = Math.Clamp(targetWidth / bounds.Width, 0.08d, 0.6d);
            var rounded = bounds.Round(zoom);

            var pixelData = _document.Render(pageNumber, bounds, zoom, PixelFormats.RGBA);

            var bitmap = new WriteableBitmap(new PixelSize(rounded.Width, rounded.Height), new Vector(72, 72), PixelFormat.Rgba8888, AlphaFormat.Unpremul);
            using (var fb = bitmap.Lock())
            {
                Marshal.Copy(pixelData, 0, fb.Address, pixelData.Length);
            }

            return new PageThumbnail(pageNumber, bitmap);
        }
    }

    partial void OnActivePageIndexChanged(int value)
    {
        if (_renderer is not null)
        {
            _renderer.PageNumber = Math.Clamp(value, 0, Math.Max(0, PageCount - 1));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _renderer?.ReleaseResources();
        _renderer = null;
        _document?.Dispose();
        _context?.Dispose();
    }

    public record PageThumbnail(int PageNumber, Bitmap Image)
    {
        public string Title => $"Page {PageNumber + 1}";
    }
}