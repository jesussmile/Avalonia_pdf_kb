using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Views;

public partial class PdfViewerView : UserControl
{
    public PdfViewerView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        AttachRendererIfPossible();
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        AttachRendererIfPossible();
    }

    private void AttachRendererIfPossible()
    {
        if (DataContext is PdfViewerViewModel vm && PdfRenderer is not null)
        {
            vm.AttachRenderer(PdfRenderer);
        }
    }
}