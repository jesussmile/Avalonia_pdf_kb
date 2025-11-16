using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AvaloniaHello.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly Action? _openPdfCallback;

    public MainViewModel(Action? openPdfCallback = null)
    {
        _openPdfCallback = openPdfCallback;
        OpenPdfCommand = new RelayCommand(() => _openPdfCallback?.Invoke(), () => _openPdfCallback is not null);
    }

    [ObservableProperty]
    private string _greeting = "Ify EFB Avalonia! test";

    [ObservableProperty]
    private string _userMessage = string.Empty;

    public IRelayCommand OpenPdfCommand { get; }
}
