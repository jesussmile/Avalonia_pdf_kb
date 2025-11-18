using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Controls;

namespace AvaloniaHello.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly Action? _openPdfCallback;
    private readonly Action? _openMapsCallback;

    public MainViewModel(Action? openPdfCallback = null, Action? openMapsCallback = null)
    {
        _openPdfCallback = openPdfCallback;
        _openMapsCallback = openMapsCallback;
        OpenPdfCommand = new RelayCommand(() => _openPdfCallback?.Invoke(), () => _openPdfCallback is not null);
        OpenMapsCommand = new RelayCommand(() => _openMapsCallback?.Invoke(), () => _openMapsCallback is not null);
        OpenAirportInfoCommand = new RelayCommand(OpenAirportInfo);
    }

    [ObservableProperty]
    private string _greeting = "Ify EFB Avalonia! test";

    [ObservableProperty]
    private string _userMessage = string.Empty;

    public IRelayCommand OpenPdfCommand { get; }

    public IRelayCommand OpenMapsCommand { get; }

    public IRelayCommand OpenAirportInfoCommand { get; }

    private void OpenAirportInfo()
    {
        var window = new AvaloniaHello.Views.AirportInfoWindow();
        window.Show();
    }
}
