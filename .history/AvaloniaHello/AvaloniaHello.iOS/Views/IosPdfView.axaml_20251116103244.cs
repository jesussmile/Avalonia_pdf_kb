using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaHello.iOS.ViewModels;

namespace AvaloniaHello.iOS.Views;

public partial class IosPdfView : UserControl
{
    public IosPdfView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is IosPdfViewModel vm)
        {
            vm.Activate();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is IosPdfViewModel vm && IsLoaded)
        {
            vm.Activate();
        }
    }
}
