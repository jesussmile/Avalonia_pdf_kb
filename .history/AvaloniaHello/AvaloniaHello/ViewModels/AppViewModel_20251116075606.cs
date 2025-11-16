using System;
using System.Diagnostics;
using System.IO;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaHello.ViewModels;

public partial class AppViewModel : ViewModelBase
{
    private readonly string _pdfFilePath;

    public AppViewModel()
    {
        _pdfFilePath = ResolvePdfPath();

        ShowPdfViewerCommand = new RelayCommand(ShowPdfViewer, () => File.Exists(_pdfFilePath));
        ShowHomeCommand = new RelayCommand(ShowHome);

        HomeViewModel = new MainViewModel(ShowPdfViewer);
        PdfViewerViewModel = new PdfViewerViewModel(_pdfFilePath, ShowHome);

        CurrentViewModel = HomeViewModel;
    }

    [ObservableProperty]
    private ViewModelBase _currentViewModel = null!;

    public MainViewModel HomeViewModel { get; }

    public PdfViewerViewModel PdfViewerViewModel { get; }

    public IRelayCommand ShowPdfViewerCommand { get; }

    public IRelayCommand ShowHomeCommand { get; }

    private void ShowHome()
    {
        CurrentViewModel = HomeViewModel;
    }

    private void ShowPdfViewer()
    {
        if (!File.Exists(_pdfFilePath))
        {
            return;
        }

        if (PdfViewerViewModel.IsInitialized is false)
        {
            PdfViewerViewModel.LoadDocument();
        }

        CurrentViewModel = PdfViewerViewModel;
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