using System;
using System.IO;
using System.Threading.Tasks;
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

    public AndroidPdfViewModel(string filePath, Action? navigateBack)
    {
        _filePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        _navigateBack = navigateBack;

        BackCommand = new RelayCommand(() => _navigateBack?.Invoke());
        NextPageCommand = new AsyncRelayCommand(NextPageAsync, () => CanMoveTo(CurrentPageIndex + 1));
        PreviousPageCommand = new AsyncRelayCommand(PreviousPageAsync, () => CanMoveTo(CurrentPageIndex - 1));
    }

    public IRelayCommand BackCommand { get; }

    public IAsyncRelayCommand NextPageCommand { get; }

    public IAsyncRelayCommand PreviousPageCommand { get; }

    public AvaloniaBitmap? CurrentPageImage
    {
        get => _currentPageImage;
        private set => SetCurrentPage(value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
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
            }
        }
    }

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

            PageCount = _document.PageCount;
            await LoadPageAsync(0);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
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
            var (image, resolvedIndex) = await Task.Run(() => RenderPage(targetIndex));
            CurrentPageImage = image;
            CurrentPageIndex = resolvedIndex;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private (AvaloniaBitmap image, int index) RenderPage(int requestedIndex)
    {
        if (_document is null)
        {
            throw new InvalidOperationException("PDF renderer is not ready");
        }

        var pageIndex = Math.Clamp(requestedIndex, 0, Math.Max(0, _document.PageCount - 1));
        using var androidBitmap = _document.RenderPage(pageIndex, 1.4f);
        var avaloniaBitmap = androidBitmap.ToAvaloniaBitmap() ?? throw new InvalidOperationException("Failed to convert PDF bitmap");
        return (avaloniaBitmap, pageIndex);
    }

    private void SetCurrentPage(AvaloniaBitmap? newImage)
    {
        var previous = _currentPageImage;
        if (SetProperty(ref _currentPageImage, newImage))
        {
            previous?.Dispose();
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
    }

    public void Dispose()
    {
        _currentPageImage?.Dispose();
        _currentPageImage = null;
        _document?.Dispose();
        _document = null;
    }
}
