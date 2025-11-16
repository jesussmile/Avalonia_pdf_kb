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
        Console.WriteLine("=== AppViewModel Constructor ===");
        _pdfPresenter = pdfPresenter ?? throw new ArgumentNullException(nameof(pdfPresenter));
        _pdfFilePath = ResolvePdfPath();
   
        Console.WriteLine($"Resolved PDF path: {_pdfFilePath}");
        Console.WriteLine($"PDF file exists: {File.Exists(_pdfFilePath)}");
        if (File.Exists(_pdfFilePath))
        {
            var fileInfo = new FileInfo(_pdfFilePath);
            Console.WriteLine($"PDF file size: {fileInfo.Length} bytes");
        }

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
        Console.WriteLine("=== ShowHome() called ===");
        CurrentViewModel = HomeViewModel;
    }

    private async Task ShowPdfViewerAsync()
    {
        Console.WriteLine("=== ShowPdfViewerAsync() called ===");
        Console.WriteLine($"PDF path: {_pdfFilePath}");
        Console.WriteLine($"File exists: {File.Exists(_pdfFilePath)}");
        
        if (!File.Exists(_pdfFilePath))
        {
            Console.WriteLine("ERROR: PDF file not found, cannot show viewer");
            return;
        }

        if (_embeddedPdfViewModel is null)
        {
            Console.WriteLine("Creating new embedded PDF ViewModel");
            if (!_pdfPresenter.TryCreateEmbeddedViewModel(_pdfFilePath, ShowHome, out var createdViewModel))
            {
                Console.WriteLine("Failed to create embedded viewer, trying external presentation");
                await _pdfPresenter.TryPresentExternallyAsync(_pdfFilePath);
                return;
            }

            _embeddedPdfViewModel = createdViewModel;
            Console.WriteLine($"Created ViewModel type: {createdViewModel?.GetType().Name ?? "null"}");
        }
        else
        {
            Console.WriteLine("Reusing existing embedded PDF ViewModel");
        }

        if (_embeddedPdfViewModel is IPdfDocumentViewModel pdfDocumentViewModel)
        {
            Console.WriteLine("Activating PDF document ViewModel");
            pdfDocumentViewModel.Activate();
        }
        else
        {
            Console.WriteLine($"WARNING: ViewModel is not IPdfDocumentViewModel, type: {_embeddedPdfViewModel?.GetType().Name ?? "null"}");
        }

        Console.WriteLine("Setting CurrentViewModel to PDF viewer");
        CurrentViewModel = _embeddedPdfViewModel;
        Console.WriteLine("PDF viewer should now be displayed");
    }

    private static string ResolvePdfPath()
    {
        Console.WriteLine("=== ResolvePdfPath() ===");
        const string fileName = "GDL90_Public_ICD_RevA.PDF";
        
        var appBaseFile = Path.Combine(AppContext.BaseDirectory, "Assets", "docs", fileName);
        Console.WriteLine($"Checking app base path: {appBaseFile}");
        Console.WriteLine($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");

        if (File.Exists(appBaseFile))
        {
            Console.WriteLine("Found PDF in app base directory");
            return appBaseFile;
        }

        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "docs");
        Console.WriteLine($"Cache directory: {cacheDir}");
        
        Directory.CreateDirectory(cacheDir);
        var cachedFile = Path.Combine(cacheDir, fileName);
        Console.WriteLine($"Cache file path: {cachedFile}");

        if (!File.Exists(cachedFile))
        {
            Console.WriteLine("PDF not in cache, attempting to extract from assets");
            TryExtractPdfAsset(cachedFile);
        }
        else
        {
            Console.WriteLine("Found PDF in cache");
        }

        return cachedFile;
    }

    private static void TryExtractPdfAsset(string destinationPath)
    {
        try
        {
            var assetUri = new Uri("avares://AvaloniaHello/Assets/docs/GDL90_Public_ICD_RevA.PDF");
            Console.WriteLine($"Checking asset: {assetUri}");

            if (!AssetLoader.Exists(assetUri))
            {
                Console.WriteLine("ERROR: Asset does not exist at specified URI");
                return;
            }

            Console.WriteLine("Asset found, extracting...");
            using var assetStream = AssetLoader.Open(assetUri);
            using var fileStream = File.Create(destinationPath);
            assetStream.CopyTo(fileStream);
            Console.WriteLine($"Successfully extracted PDF to: {destinationPath}");
            Console.WriteLine($"Extracted file size: {new FileInfo(destinationPath).Length} bytes");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: Failed to extract PDF asset: {ex.GetType().Name}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }
}