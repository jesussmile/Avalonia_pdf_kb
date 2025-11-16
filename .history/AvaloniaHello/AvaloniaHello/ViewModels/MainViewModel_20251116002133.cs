using CommunityToolkit.Mvvm.ComponentModel;

namespace AvaloniaHello.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _greeting = "Hello from Avalonia everywhere!";
}
