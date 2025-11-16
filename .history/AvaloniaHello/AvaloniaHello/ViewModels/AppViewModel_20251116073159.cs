using System;
using System.IO;
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
        var baseDir = AppContext.BaseDirectory;
        return Path.Combine(baseDir, "Assets", "docs", "GDL90_Public_ICD_RevA.PDF");
    }
}