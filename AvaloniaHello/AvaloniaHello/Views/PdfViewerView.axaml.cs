using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Views;

public partial class PdfViewerView : UserControl
{
    private bool _hasAttachedRenderer;

    public PdfViewerView()
    {
        Console.WriteLine("=== PdfViewerView Constructor ===");

        try
        {
            InitializeComponent();
            Console.WriteLine("PdfViewerView: InitializeComponent succeeded");
            Console.WriteLine($"PdfRenderer after InitializeComponent: {(PdfRenderer != null ? "Found" : "NULL")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CRITICAL ERROR: PdfViewerView InitializeComponent failed - {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");

            Content = new TextBlock
            {
                Text = $"Failed to load PDF viewer: {ex.GetType().Name}: {ex.Message}",
                Foreground = Brushes.White,
                Margin = new Thickness(16)
            };
            return;
        }

        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        Console.WriteLine($"=== PdfViewerView: DataContext changed ===");
        Console.WriteLine($"DataContext type: {DataContext?.GetType().Name ?? "null"}");

        if (DataContext is PdfViewerViewModel vm)
        {
            Console.WriteLine($"DataContext is PdfViewerViewModel");
            Console.WriteLine($"PdfRenderer control: {(PdfRenderer != null ? "Found" : "NULL")}");
        }
        else
        {
            Console.WriteLine($"DataContext is NOT PdfViewerViewModel: {DataContext?.GetType().Name ?? "null"}");
        }

        // Try to attach if view is already loaded
        if (IsLoaded)
        {
            AttachRendererIfPossible();
        }
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Console.WriteLine("=== PdfViewerView: Loaded event fired ===");
        Console.WriteLine($"DataContext: {DataContext?.GetType().Name ?? "null"}");
        Console.WriteLine($"PdfRenderer control: {(PdfRenderer != null ? "Found" : "NULL")}");

        // Use Dispatcher to ensure the visual tree is fully initialized
        Dispatcher.UIThread.Post(() =>
        {
            Console.WriteLine("=== Post-Loaded: Attempting renderer attachment ===");
            Console.WriteLine($"PdfRenderer after layout: {(PdfRenderer != null ? "Found" : "NULL")}");
            AttachRendererIfPossible();
        }, DispatcherPriority.Loaded);
    }

    private void AttachRendererIfPossible()
    {
        if (_hasAttachedRenderer)
        {
            Console.WriteLine("=== AttachRendererIfPossible: Already attached, skipping ===");
            return;
        }

        Console.WriteLine("=== AttachRendererIfPossible() ===");
        Console.WriteLine($"  - IsLoaded: {IsLoaded}");
        Console.WriteLine($"  - DataContext type: {DataContext?.GetType().Name ?? "null"}");
        Console.WriteLine($"  - PdfRenderer is not null: {PdfRenderer is not null}");

        if (DataContext is PdfViewerViewModel vm && PdfRenderer is not null)
        {
            Console.WriteLine("✓ Conditions met: Attaching renderer to ViewModel");
            _hasAttachedRenderer = true;
            vm.AttachRenderer(PdfRenderer);
        }
        else
        {
            Console.WriteLine($"✗ Cannot attach renderer:");
            Console.WriteLine($"  - DataContext is PdfViewerViewModel: {DataContext is PdfViewerViewModel}");
            Console.WriteLine($"  - PdfRenderer is not null: {PdfRenderer is not null}");
        }
    }
}