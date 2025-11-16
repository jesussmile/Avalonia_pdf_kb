using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaHello.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Ify EFB Avalonia! test";

    [ObservableProperty]
    private string _userMessage = string.Empty;
}
