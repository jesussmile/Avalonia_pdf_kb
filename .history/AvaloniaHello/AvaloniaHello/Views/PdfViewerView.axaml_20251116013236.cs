using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Views;

public partial class PdfViewerView : UserControl
{
    public PdfViewerView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        AttachRendererIfPossible();
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
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