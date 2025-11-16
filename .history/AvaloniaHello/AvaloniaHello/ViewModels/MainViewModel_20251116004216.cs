using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaHello.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Hello from Avalonia";

    [ObservableProperty]
    private string _userMessage = string.Empty;
}
