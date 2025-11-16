using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Views;

public partial class AndroidPdfView : UserControl
{
    public AndroidPdfView()
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
        if (DataContext is AndroidPdfViewModel vm)
        {
            vm.Activate();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AndroidPdfViewModel vm && IsLoaded)
        {
            vm.Activate();
        }
    }
}
