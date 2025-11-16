using System;
using System.Diagnostics;
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
        Debug.WriteLine("AndroidPdfView: ctor initialized");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
        Debug.WriteLine("AndroidPdfView: XAML loaded");
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is AndroidPdfViewModel vm)
        {
            Debug.WriteLine("AndroidPdfView: Loaded event activating view model");
            vm.Activate();
        }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is AndroidPdfViewModel vm && IsLoaded)
        {
            Debug.WriteLine("AndroidPdfView: DataContext changed, activating");
            vm.Activate();
        }
    }
}
