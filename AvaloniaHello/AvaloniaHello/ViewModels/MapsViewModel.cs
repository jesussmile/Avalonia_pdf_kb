using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Animations;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;

namespace AvaloniaHello.ViewModels;

public partial class MapsViewModel : ViewModelBase
{
    private readonly Action? _goHomeCallback;

    public MapsViewModel(Action? goHomeCallback = null)
    {
        _goHomeCallback = goHomeCallback;
        GoHomeCommand = new RelayCommand(() => _goHomeCallback?.Invoke(), () => _goHomeCallback is not null);
        Map = CreateSampleMap();
    }

    public Map Map { get; }

    public IRelayCommand GoHomeCommand { get; }

    private static Map CreateSampleMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        var (cityX, cityY) = SphericalMercator.FromLonLat(-73.9857, 40.7484); // Midtown Manhattan
        var (statueX, statueY) = SphericalMercator.FromLonLat(-74.0445, 40.6892);
        var (parkX, parkY) = SphericalMercator.FromLonLat(-73.9654, 40.7829);

        var cityCenter = new MPoint(cityX, cityY);
        var statue = new MPoint(statueX, statueY);
        var park = new MPoint(parkX, parkY);

        var markerLayer = new MemoryLayer("Points of Interest")
        {
            Features = new List<IFeature>
            {
                CreatePointFeature(cityCenter, "Times Square"),
                CreatePointFeature(statue, "Statue of Liberty"),
                CreatePointFeature(park, "Central Park")
            },
            Style = new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.8,
                Fill = new Brush { Color = Color.FromArgb(255, 30, 144, 255) },
                Outline = new Pen(Color.White, 2)
            }
        };

        map.Layers.Add(markerLayer);
        map.Home = navigator =>
        {
            navigator.CenterOnAndZoomTo(cityCenter, 6000, 0, Easing.Linear);
        };

        return map;
    }

    private static IFeature CreatePointFeature(MPoint position, string label)
    {
        var feature = new PointFeature(position)
        {
            ["Label"] = label
        };

        return feature;
    }
}
