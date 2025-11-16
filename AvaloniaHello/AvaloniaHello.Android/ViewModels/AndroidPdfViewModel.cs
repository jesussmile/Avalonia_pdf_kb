using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvaloniaHello.Android;
using CommunityToolkit.Mvvm.Input;
using AvaloniaBitmap = Avalonia.Media.Imaging.Bitmap;

namespace AvaloniaHello.ViewModels;

internal sealed class AndroidPdfViewModel : ViewModelBase, IPdfDocumentViewModel, IDisposable
{
    private readonly string _filePath;
    private readonly Action? _navigateBack;
    private AndroidPdfDocument? _document;
    private bool _isActivated;
    private AvaloniaBitmap? _currentPageImage;
    private bool _isBusy;
    private int _pageCount;
    private int _currentPageIndex;
    private string? _errorMessage;
    private double _zoom = 1.25;
    private readonly ObservableCollection<PageThumbnail> _thumbnails = new();
    private readonly ReadOnlyObservableCollection<PageThumbnail> _readonlyThumbnails;
    private Task? _thumbnailWarmupTask;
    private bool _isThumbnailMode;
    private bool _isLoadingThumbnails;

    public AndroidPdfViewModel(string filePath, Action? navigateBack)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _navigateBack = navigateBack;

        BackCommand = new RelayCommand(() => _navigateBack?.Invoke());
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => CanMoveTo(CurrentPageIndex + 1));
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => CanMoveTo(CurrentPageIndex - 1));
        SelectPageCommand = new AsyncRelayCommand<int>(SelectPageAsync, CanMoveTo);
        _readonlyThumbnails = new ReadOnlyObservableCollection<PageThumbnail>(_thumbnails);
    }

    public IRelayCommand BackCommand { get; }

    public IAsyncRelayCommand NextPageCommand { get; }

    public IAsyncRelayCommand PreviousPageCommand { get; }

    public IAsyncRelayCommand<int> SelectPageCommand { get; }

    public AvaloniaBitmap? CurrentPageImage
    {
        get => _currentPageImage;
        private set => SetCurrentPage(value);
    }

    public bool HasImage => _currentPageImage is not null;

    public string ImageInfo => _currentPageImage is null
        ? "(no image)"
        : $"{_currentPageImage.PixelSize.Width}x{_currentPageImage.PixelSize.Height}";

    public double Zoom
    {
        get => _zoom;
        set
        {
            if (SetProperty(ref _zoom, Math.Clamp(value, 0.75, 2.5)))
            {
                _ = LoadPageAsync(CurrentPageIndex);
                OnPropertyChanged(nameof(ZoomWidth));
            }
        }
    }

    public double ZoomWidth => (_currentPageImage?.PixelSize.Width ?? 0) * Zoom;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public bool IsLoadingThumbnails
    {
        get => _isLoadingThumbnails;
        private set => SetProperty(ref _isLoadingThumbnails, value);
    }

    public int PageCount
    {
        get => _pageCount;
        private set
        {
            if (SetProperty(ref _pageCount, value))
            {
                UpdateNavigationStates();
            }
        }
    }

    public int CurrentPageIndex
    {
        get => _currentPageIndex;
        private set
        {
            if (SetProperty(ref _currentPageIndex, value))
            {
                UpdateNavigationStates();
                OnPropertyChanged(nameof(DisplayPageNumber));
            }
        }
    }

    public int DisplayPageNumber => CurrentPageIndex + 1;

    public bool IsThumbnailMode
    {
        get => _isThumbnailMode;
        set
        {
            if (SetProperty(ref _isThumbnailMode, value))
            {
                OnPropertyChanged(nameof(IsSinglePageMode));
                if (value)
                {
                    _ = EnsureThumbnailsAsync();
                }
            }
        }
    }

    public bool IsSinglePageMode => !IsThumbnailMode;

    public ReadOnlyObservableCollection<PageThumbnail> PageThumbnails => _readonlyThumbnails;

    public bool HasThumbnails => _thumbnails.Count > 0;

    public bool HasNoThumbnails => !HasThumbnails;

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                OnPropertyChanged(nameof(HasError));
            }
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

    public void Activate()
    {
        if (_isActivated)
        {
            if (_document is not null && CurrentPageImage is null)
            {
                _ = LoadPageAsync(CurrentPageIndex);
            }
            return;
        }

        _isActivated = true;
        _ = LoadDocumentAsync();
    }

    private async Task LoadDocumentAsync()
    {
        if (!File.Exists(_filePath))
        {
            ErrorMessage = "PDF file not found on device";
            return;
        }

        IsBusy = true;
        ErrorMessage = null;

        try
        {
            _document = await Task.Run(() => AndroidPdfDocument.TryOpen(_filePath));

            if (_document is null)
            {
                ErrorMessage = "Unable to open PDF on Android";
                return;
            }

            ResetThumbnails();
            PageCount = _document.PageCount;
            await LoadPageAsync(0);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
            Debug.WriteLine($"AndroidPdfViewModel: LoadDocumentAsync failed - {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Task NextPageAsync()
    {
        return LoadPageAsync(CurrentPageIndex + 1);
    }

    private Task PreviousPageAsync()
    {
        return LoadPageAsync(CurrentPageIndex - 1);
    }

    private async Task LoadPageAsync(int targetIndex)
    {
        if (_document is null)
        {
            return;
        }

        IsBusy = true;

        try
        {
            var zoomFactor = Zoom;
            var (androidBitmap, resolvedIndex) = await Task.Run(() => RenderAndroidBitmap(targetIndex, zoomFactor));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                using (androidBitmap)
                {
                    var avaloniaBitmap = androidBitmap.ToAvaloniaBitmap() ??
                        throw new InvalidOperationException("Failed to convert PDF bitmap");

                    CurrentPageImage = avaloniaBitmap;
                    CurrentPageIndex = resolvedIndex;
                }
            });
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.ToString();
            Debug.WriteLine($"AndroidPdfViewModel: LoadPageAsync failed - {ex}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task SelectPageAsync(int pageIndex)
    {
        await LoadPageAsync(pageIndex);
        IsThumbnailMode = false;
    }

    private (global::Android.Graphics.Bitmap bitmap, int index) RenderAndroidBitmap(int requestedIndex, double zoom)
    {
        if (_document is null)
        {
            throw new InvalidOperationException("PDF renderer is not ready");
        }

        var pageIndex = Math.Clamp(requestedIndex, 0, Math.Max(0, _document.PageCount - 1));
        var bitmap = _document.RenderPage(pageIndex, (float)(1.0 * zoom));
        return (bitmap, pageIndex);
    }

    private void SetCurrentPage(AvaloniaBitmap? newImage)
    {
        var previous = _currentPageImage;
        if (SetProperty(ref _currentPageImage, newImage, nameof(CurrentPageImage)))
        {
            previous?.Dispose();
            Debug.WriteLine($"AndroidPdfViewModel: Updated page image to {newImage?.PixelSize.Width}x{newImage?.PixelSize.Height}");
            OnPropertyChanged(nameof(HasImage));
            OnPropertyChanged(nameof(ImageInfo));
            OnPropertyChanged(nameof(ZoomWidth));
        }
        else
        {
            newImage?.Dispose();
        }
    }

    private bool CanMoveTo(int index)
    {
        return index >= 0 && index < PageCount;
    }

    private void UpdateNavigationStates()
    {
        NextPageCommand.NotifyCanExecuteChanged();
        PreviousPageCommand.NotifyCanExecuteChanged();
        SelectPageCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _currentPageImage?.Dispose();
        _currentPageImage = null;
        _document?.Dispose();
        _document = null;
        ResetThumbnails();
    }

    private async Task EnsureThumbnailsAsync()
    {
        if (_document is null || HasThumbnails)
        {
            return;
        }

        if (_thumbnailWarmupTask is not null)
        {
            await _thumbnailWarmupTask;
            return;
        }

        IsLoadingThumbnails = true;

        _thumbnailWarmupTask = Task.Run(async () =>
        {
            if (_document is null)
            {
                return;
            }

            for (var i = 0; i < _document.PageCount; i++)
            {
                if (_document is null)
                {
                    return;
                }

                var (androidBitmap, resolvedIndex) = RenderAndroidBitmap(i, 0.3);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    using (androidBitmap)
                    {
                        var avaloniaBitmap = androidBitmap.ToAvaloniaBitmap() ??
                            throw new InvalidOperationException("Failed to convert PDF thumbnail");

                        _thumbnails.Add(new PageThumbnail(resolvedIndex, avaloniaBitmap));
                        OnPropertyChanged(nameof(HasThumbnails));
                        OnPropertyChanged(nameof(HasNoThumbnails));
                    }
                });
            }
        });

        try
        {
            await _thumbnailWarmupTask;
        }
        finally
        {
            IsLoadingThumbnails = false;
            _thumbnailWarmupTask = null;
        }
    }

    private void ResetThumbnails()
    {
        if (_thumbnails.Count == 0)
        {
            return;
        }

        foreach (var thumbnail in _thumbnails)
        {
            thumbnail.Dispose();
        }

        _thumbnails.Clear();
        OnPropertyChanged(nameof(HasThumbnails));
        OnPropertyChanged(nameof(HasNoThumbnails));
    }

    public sealed class PageThumbnail : IDisposable
    {
        public PageThumbnail(int pageIndex, AvaloniaBitmap image)
        {
            PageIndex = pageIndex;
            Image = image ?? throw new ArgumentNullException(nameof(image));
        }

        public int PageIndex { get; }

        public AvaloniaBitmap Image { get; }

        public int DisplayNumber => PageIndex + 1;

        public void Dispose()
        {
            Image.Dispose();
        }
    }
}
