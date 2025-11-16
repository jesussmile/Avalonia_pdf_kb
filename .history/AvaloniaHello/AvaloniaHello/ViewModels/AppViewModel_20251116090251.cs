using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AvaloniaHello.Services;

namespace AvaloniaHello.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    private readonly string _pdfFilePath;
    private readonly IPdfPresenter _pdfPresenter;
    private ViewModelBase? _embeddedPdfViewModel;

    public AppViewModel()
        : this(PdfPresenter.Current)
    {
    }

    public AppViewModel(IPdfPresenter pdfPresenter)
    {
        _pdfPresenter = pdfPresenter ?? throw new ArgumentNullException(nameof(pdfPresenter));
        _pdfFilePath = ResolvePdfPath();

        ShowPdfViewerCommand = new AsyncRelayCommand(ShowPdfViewerAsync, () => File.Exists(_pdfFilePath));
        ShowHomeCommand = new RelayCommand(ShowHome);

        HomeViewModel = new MainViewModel(() => ShowPdfViewerCommand.Execute(null));

        CurrentViewModel = HomeViewModel;
    }

    [ObservableProperty]
    private ViewModelBase _currentViewModel = null!;

    public MainViewModel HomeViewModel { get; }

    public IAsyncRelayCommand ShowPdfViewerCommand { get; }

    public IRelayCommand ShowHomeCommand { get; }

    private void ShowHome()
    {
        CurrentViewModel = HomeViewModel;
    }

    private async Task ShowPdfViewerAsync()
    {
        if (!File.Exists(_pdfFilePath))
        {
            return;
        }

        if (_embeddedPdfViewModel is null)
        {
            if (!_pdfPresenter.TryCreateEmbeddedViewModel(_pdfFilePath, ShowHome, out var createdViewModel))
            {
                await _pdfPresenter.TryPresentExternallyAsync(_pdfFilePath);
                return;
            }

            _embeddedPdfViewModel = createdViewModel;
        }

        if (_embeddedPdfViewModel is IPdfDocumentViewModel pdfDocumentViewModel)
        {
            pdfDocumentViewModel.Activate();
        }

        CurrentViewModel = _embeddedPdfViewModel;
    }

    private static string ResolvePdfPath()
    {
        const string fileName = "GDL90_Public_ICD_RevA.PDF";
        var appBaseFile = Path.Combine(AppContext.BaseDirectory, "Assets", "docs", fileName);

        if (File.Exists(appBaseFile))
        {
            return appBaseFile;
        }

        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "docs");
        Directory.CreateDirectory(cacheDir);
        var cachedFile = Path.Combine(cacheDir, fileName);

        if (!File.Exists(cachedFile))
        {
            TryExtractPdfAsset(cachedFile);
        }

        return cachedFile;
    }

    private static void TryExtractPdfAsset(string destinationPath)
    {
        try
        {
            var assetUri = new Uri("avares://AvaloniaHello/Assets/docs/GDL90_Public_ICD_RevA.PDF");

            if (!AssetLoader.Exists(assetUri))
            {
                return;
            }

            using var assetStream = AssetLoader.Open(assetUri);
            using var fileStream = File.Create(destinationPath);
            assetStream.CopyTo(fileStream);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to extract PDF asset: {ex.Message}");
        }
    }
}