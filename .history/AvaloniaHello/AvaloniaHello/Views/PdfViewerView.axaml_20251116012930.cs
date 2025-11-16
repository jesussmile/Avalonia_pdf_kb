using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Views;

public partial class PdfViewerView : UserControl
{
    public PdfViewerView()
    {
        InitializeComponent();
        AttachedToVisualTree += OnAttachedToVisualTree;
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (DataContext is PdfViewerViewModel vm && PdfRenderer is not null)
        {
            vm.AttachRenderer(PdfRenderer);
        }
    }
}