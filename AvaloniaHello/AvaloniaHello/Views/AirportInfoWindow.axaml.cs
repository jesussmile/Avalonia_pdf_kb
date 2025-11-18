using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using System;

namespace AvaloniaHello.Views;

public partial class AirportInfoWindow : Window
{
    public AirportInfoWindow()
    {
        InitializeComponent();
        
        var thumbnail = this.FindControl<Border>("RunwayThumbnail");
        if (thumbnail != null)
        {
            thumbnail.PointerPressed += RunwayThumbnail_Click;
        }

        var closeBtn = this.FindControl<Button>("CloseRunwayDiagramBtn");
        if (closeBtn != null)
        {
            closeBtn.Click += CloseRunwayDiagram_Click;
        }
    }

    private void RunwayThumbnail_Click(object? sender, PointerPressedEventArgs e)
    {
        var thumbnail = this.FindControl<Border>("RunwayThumbnail");
        var expandedDiagram = this.FindControl<Border>("RunwayDiagramExpanded");
        
        if (thumbnail != null && expandedDiagram != null)
        {
            thumbnail.IsVisible = false;
            expandedDiagram.IsVisible = true;
        }
    }

    private void CloseRunwayDiagram_Click(object? sender, RoutedEventArgs e)
    {
        var thumbnail = this.FindControl<Border>("RunwayThumbnail");
        var expandedDiagram = this.FindControl<Border>("RunwayDiagramExpanded");
        
        if (thumbnail != null && expandedDiagram != null)
        {
            expandedDiagram.IsVisible = false;
            thumbnail.IsVisible = true;
        }
    }
}