using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaHello.ViewModels;
using Mapsui.Animations;

namespace AvaloniaHello.Views;

public partial class MapsView : UserControl
{
    private const string RouteStopDataFormat = "AvaloniaHello.RouteStop";
    private bool _shouldClearPicker;

    public MapsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        MapHost.Loaded += OnMapHostLoaded;
        MapHost.Info += OnMapInfo;
        
        var closeBtn = this.FindControl<Button>("CloseInfoBtn");
        if (closeBtn != null)
        {
            closeBtn.Click += OnCloseInfoClick;
        }
        
        var runwayThumbnail = this.FindControl<Border>("RunwayThumbnail");
        if (runwayThumbnail != null)
        {
            runwayThumbnail.PointerPressed += OnRunwayThumbnailClick;
        }
        
        var closeRunwayBtn = this.FindControl<Button>("CloseRunwayDiagramBtn");
        if (closeRunwayBtn != null)
        {
            closeRunwayBtn.Click += OnCloseRunwayDiagramClick;
        }
        
        ApplyMapFromDataContext();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        ApplyMapFromDataContext();
    }

    private void ApplyMapFromDataContext()
    {
        if (DataContext is not MapsViewModel viewModel)
        {
            return;
        }

        MapHost.Map = viewModel.Map;
        ResetMapViewport(viewModel);
    }

    private void OnMapHostLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MapsViewModel viewModel)
        {
            return;
        }

        ResetMapViewport(viewModel);
    }

    private void ResetMapViewport(MapsViewModel viewModel)
    {
        var navigator = viewModel.Map?.Navigator;
        if (navigator is null)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            navigator.CenterOnAndZoomTo(viewModel.HomeCenter, viewModel.HomeResolution, 0, Easing.Linear);
        }, DispatcherPriority.Background);
    }

    private void OnAirportSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (sender is not AutoCompleteBox picker || DataContext is not MapsViewModel viewModel)
        {
            return;
        }

        if (picker.SelectedItem is MapsViewModel.NavMarker marker)
        {
            viewModel.TryAddRouteStop(marker);
            _shouldClearPicker = true;
            
            Dispatcher.UIThread.Post(() =>
            {
                if (_shouldClearPicker)
                {
                    ResetAirportPicker(picker);
                    _shouldClearPicker = false;
                }
            }, DispatcherPriority.Loaded);
        }
    }

    private static void ResetAirportPicker(AutoCompleteBox picker)
    {
        picker.SelectedItem = null;
        picker.Text = string.Empty;
        
        // Find and clear the internal TextBox
        var textBox = picker.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        if (textBox != null)
        {
            textBox.Text = string.Empty;
            textBox.Clear();
        }
    }

    private async void OnChipPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control control || DataContext is not MapsViewModel)
        {
            return;
        }

        var point = e.GetCurrentPoint(control);
        if (!point.Properties.IsLeftButtonPressed)
        {
            return;
        }

        if (control.DataContext is not MapsViewModel.RouteStop routeStop)
        {
            return;
        }

        // Set dragging state
        routeStop.IsDragging = true;

        var dataObject = new DataObject();
        dataObject.Set(RouteStopDataFormat, routeStop);

        await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);

        // Reset dragging state after drag operation
        routeStop.IsDragging = false;
    }

    private void OnChipDragOver(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(RouteStopDataFormat))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnChipDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MapsViewModel viewModel)
        {
            return;
        }

        if (!e.Data.Contains(RouteStopDataFormat))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        if (e.Data.Get(RouteStopDataFormat) is not MapsViewModel.RouteStop routeStop)
        {
            return;
        }

        var target = (sender as Control)?.DataContext as MapsViewModel.RouteStop;
        var placeAfter = false;

        if (sender is Control control)
        {
            var position = e.GetPosition(control);
            placeAfter = position.X > control.Bounds.Width / 2;
        }

        viewModel.MoveRouteStop(routeStop, target, placeAfter);
        e.Handled = true;
    }

    private void OnChipContainerDragOver(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(RouteStopDataFormat))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        e.DragEffects = DragDropEffects.Move;
        e.Handled = true;
    }

    private void OnChipContainerDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MapsViewModel viewModel)
        {
            return;
        }

        if (!e.Data.Contains(RouteStopDataFormat))
        {
            e.DragEffects = DragDropEffects.None;
            return;
        }

        if (e.Data.Get(RouteStopDataFormat) is not MapsViewModel.RouteStop routeStop)
        {
            return;
        }

        viewModel.MoveRouteStop(routeStop, null, true);
        e.Handled = true;
    }

    private void OnMapInfo(object? sender, EventArgs e)
    {
        Console.WriteLine($"Map Info event triggered - showing KFTW panel");
        
        var panel = this.FindControl<Border>("AirportInfoPanel");
        if (panel != null)
        {
            panel.IsVisible = true;
        }

        // Highlight KFTW marker with animated blue glow and pan map
        if (DataContext is MapsViewModel viewModel)
        {
            viewModel.SelectAirport("KFTW");
            
            // Pan map to keep airport visible near the information panel
            // Shift left by 200 pixels to account for the 450px panel width
            viewModel.PanMapToAirport("KFTW", -200);
        }
    }

    private void OnCloseInfoClick(object? sender, RoutedEventArgs e)
    {
        var panel = this.FindControl<Border>("AirportInfoPanel");
        if (panel != null)
        {
            panel.IsVisible = false;
        }
        
        // Also hide the expanded diagram if visible
        var expandedDiagram = this.FindControl<Border>("RunwayDiagramExpanded");
        if (expandedDiagram != null)
        {
            expandedDiagram.IsVisible = false;
        }

        // Remove KFTW selection glow
        if (DataContext is MapsViewModel viewModel)
        {
            viewModel.SelectAirport(null);
        }
    }

    private void OnRunwayThumbnailClick(object? sender, PointerPressedEventArgs e)
    {
        var expandedDiagram = this.FindControl<Border>("RunwayDiagramExpanded");
        if (expandedDiagram != null)
        {
            expandedDiagram.IsVisible = true;
        }
    }

    private void OnCloseRunwayDiagramClick(object? sender, RoutedEventArgs e)
    {
        var expandedDiagram = this.FindControl<Border>("RunwayDiagramExpanded");
        if (expandedDiagram != null)
        {
            expandedDiagram.IsVisible = false;
        }
    }
}
