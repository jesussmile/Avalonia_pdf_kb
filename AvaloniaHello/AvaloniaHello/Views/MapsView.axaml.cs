using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaHello.ViewModels;
using Mapsui.Animations;

namespace AvaloniaHello.Views;

public partial class MapsView : UserControl
{
    private const string RouteStopDataFormat = "AvaloniaHello.RouteStop";

    public MapsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        MapHost.Loaded += OnMapHostLoaded;
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

        if (picker.SelectedItem is MapsViewModel.NavMarker marker && viewModel.TryAddRouteStop(marker))
        {
            ResetAirportPicker(picker);
        }
    }

    private static void ResetAirportPicker(AutoCompleteBox picker)
    {
        picker.Text = string.Empty;
        picker.SelectedItem = null;
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

        var dataObject = new DataObject();
        dataObject.Set(RouteStopDataFormat, routeStop);

        await DragDrop.DoDragDrop(e, dataObject, DragDropEffects.Move);
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
}
