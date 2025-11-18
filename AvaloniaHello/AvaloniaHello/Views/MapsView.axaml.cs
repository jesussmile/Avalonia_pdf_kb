using System;
using Avalonia.Controls;
using AvaloniaHello.ViewModels;

namespace AvaloniaHello.Views;

public partial class MapsView : UserControl
{
    public MapsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
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
    }
}
