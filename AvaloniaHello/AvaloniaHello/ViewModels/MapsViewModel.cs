using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapsui;
using Mapsui.Animations;
using Mapsui.Layers;
using Mapsui.Nts.Extensions;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling;
using NetTopologySuite.Geometries;

namespace AvaloniaHello.ViewModels;

public partial class MapsViewModel : ViewModelBase
{
    private const double DefaultRouteZoomResolution = 7000;
    private static readonly MPoint DefaultCenter = ToPoint(32.8972331, -97.0376947);

    private readonly Action? _goHomeCallback;
    private readonly ObservableCollection<RouteStop> _routeStops = new();
    private readonly MemoryLayer _routeLineLayer;
    private readonly MemoryLayer _routeMarkerLayer;
    private MemoryLayer? _airportLayer;
    private CancellationTokenSource? _glowAnimationCts;
    private double _glowAnimationPhase = 0;

    [ObservableProperty]
    private string? _selectedAirportLabel;

    public MapsViewModel(Action? goHomeCallback = null)
    {
        _goHomeCallback = goHomeCallback;
        GoHomeCommand = new RelayCommand(() => _goHomeCallback?.Invoke(), () => _goHomeCallback is not null);

        _routeLineLayer = new MemoryLayer("Route Path")
        {
            Features = new List<IFeature>(),
            Style = new VectorStyle
            {
                Line = new Pen(Color.FromArgb(255, 59, 130, 246), 4)
            }
        };

        _routeMarkerLayer = new MemoryLayer("Route Stops")
        {
            Features = new List<IFeature>(),
            Style = null
        };

        Map = CreateMap();

        RemoveRouteStopCommand = new RelayCommand<RouteStop>(stop => RemoveRouteStop(stop), stop => stop is not null);

        _routeStops.CollectionChanged += OnRouteStopsChanged;
        UpdateRouteLayers();
    }

    public Map Map { get; }

    public IRelayCommand GoHomeCommand { get; }

    public ObservableCollection<RouteStop> RouteStops => _routeStops;

    public IReadOnlyList<NavMarker> AirportSuggestions => AirportMarkers;

    public IRelayCommand<RouteStop> RemoveRouteStopCommand { get; }

    public MPoint HomeCenter => DefaultCenter;

    public double HomeResolution => DefaultRouteZoomResolution;

    public bool TryAddRouteStop(NavMarker marker)
    {
        if (marker is null)
        {
            return false;
        }

        if (_routeStops.Any(stop => string.Equals(stop.Marker.Label, marker.Label, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _routeStops.Add(new RouteStop(marker, ToPoint(marker.Latitude, marker.Longitude)));
        return true;
    }

    public void RemoveRouteStop(RouteStop? stop)
    {
        if (stop is null)
        {
            return;
        }

        _routeStops.Remove(stop);
    }

    public void MoveRouteStop(RouteStop stop, RouteStop? target, bool placeAfter)
    {
        if (stop is null)
        {
            return;
        }

        if (target is null)
        {
            if (_routeStops.Count <= 1)
            {
                return;
            }

            var oldIndex = _routeStops.IndexOf(stop);
            if (oldIndex < 0 || oldIndex == _routeStops.Count - 1)
            {
                return;
            }

            var item = _routeStops[oldIndex];
            _routeStops.RemoveAt(oldIndex);
            _routeStops.Add(item);
            return;
        }

        if (ReferenceEquals(stop, target))
        {
            return;
        }

        var old = _routeStops.IndexOf(stop);
        if (old < 0)
        {
            return;
        }

        var itemToMove = _routeStops[old];
        _routeStops.RemoveAt(old);

        var targetIndex = _routeStops.IndexOf(target);
        if (targetIndex < 0)
        {
            _routeStops.Add(itemToMove);
            return;
        }

        if (placeAfter)
        {
            targetIndex++;
        }

        if (targetIndex > _routeStops.Count)
        {
            targetIndex = _routeStops.Count;
        }

        _routeStops.Insert(targetIndex, itemToMove);
    }

    public NavMarker? FindAirportByQuery(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        return AirportMarkers.FirstOrDefault(m =>
                   string.Equals(m.Label, query, StringComparison.OrdinalIgnoreCase))
               ?? AirportMarkers.FirstOrDefault(m =>
                   string.Equals(m.Name, query, StringComparison.OrdinalIgnoreCase));
    }

    public void SelectAirport(string? airportLabel)
    {
        // Cancel any existing animation
        _glowAnimationCts?.Cancel();
        _glowAnimationCts?.Dispose();
        _glowAnimationCts = null;

        SelectedAirportLabel = airportLabel;
        
        if (!string.IsNullOrEmpty(airportLabel))
        {
            // Start glow animation
            _glowAnimationCts = new CancellationTokenSource();
            _ = AnimateGlowAsync(_glowAnimationCts.Token);
        }
        else
        {
            UpdateAirportMarkerStyles();
        }
    }

    private async Task AnimateGlowAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _glowAnimationPhase += 0.08;
                if (_glowAnimationPhase > Math.PI * 2)
                {
                    _glowAnimationPhase = 0;
                }

                UpdateAirportMarkerStyles();
                Map?.RefreshGraphics();
                await Task.Delay(40, cancellationToken);
            }
        }
        catch (TaskCanceledException)
        {
            // Animation was cancelled, this is expected
        }
    }

    public void PanMapToAirport(string airportLabel, double panOffsetX = -200)
    {
        var airport = AirportMarkers.FirstOrDefault(m => 
            string.Equals(m.Label, airportLabel, StringComparison.OrdinalIgnoreCase));
        
        if (airport == null || Map.Navigator == null) return;

        var airportPoint = ToPoint(airport.Latitude, airport.Longitude);
        
        // Calculate offset in map coordinates (shift left to make room for panel)
        var offsetInMapUnits = panOffsetX * Map.Navigator.Viewport.Resolution;
        var targetPoint = new MPoint(airportPoint.X + offsetInMapUnits, airportPoint.Y);
        
        // Animate pan to the target position
        Map.Navigator.CenterOn(targetPoint, 500, Easing.CubicOut);
    }

    private void UpdateAirportMarkerStyles()
    {
        if (_airportLayer?.Features == null) return;

        foreach (var feature in _airportLayer.Features)
        {
            var label = feature["Label"]?.ToString();
            var isSelected = !string.IsNullOrEmpty(SelectedAirportLabel) && 
                           string.Equals(label, SelectedAirportLabel, StringComparison.OrdinalIgnoreCase);

            feature.Styles.Clear();
            
            // Add base symbol style
            feature.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.8,
                Fill = null,
                Outline = new Pen(Color.FromArgb(255, 17, 94, 224), 3)
            });

            // Add animated glow effect for selected airport
            if (isSelected)
            {
                // Calculate pulsing values using sine wave
                var pulse = (Math.Sin(_glowAnimationPhase) + 1) / 2; // 0 to 1
                
                // Bright outer glow ring (visible on white background)
                feature.Styles.Add(new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = 1.6 + (pulse * 0.25), // 1.6 to 1.85
                    Fill = new Brush(Color.FromArgb((int)(100 + pulse * 60), 59, 130, 246)), // brighter pulse
                    Outline = new Pen(Color.FromArgb((int)(180 + pulse * 75), 37, 99, 235), 3)
                });
                
                // Bright center with filled marker
                feature.Styles.Add(new SymbolStyle
                {
                    SymbolType = SymbolType.Ellipse,
                    SymbolScale = 0.8,
                    Fill = new Brush(Color.FromArgb(255, 59, 130, 246)),
                    Outline = new Pen(Color.FromArgb(255, 147, 197, 253), 4) // bright blue outline
                });
            }

            // Add label style
            feature.Styles.Add(new LabelStyle
            {
                LabelColumn = "Label",
                ForeColor = Color.White,
                BackColor = new Brush { Color = Color.FromArgb(180, 17, 94, 224) },
                Halo = new Pen(Color.FromArgb(220, 0, 0, 0), 2),
                Offset = new Offset(0, -24)
            });
        }

        Map.RefreshData();
    }

    private Map CreateMap()
    {
        var map = new Map();
        map.Layers.Add(OpenStreetMap.CreateTileLayer());

        _airportLayer = CreateLayer(
            "Airports",
            AirportMarkers,
            () => new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.8,
                Fill = null,
                Outline = new Pen(Color.FromArgb(255, 17, 94, 224), 3)
            },
            () => new LabelStyle
            {
                LabelColumn = "Label",
                ForeColor = Color.White,
                BackColor = new Brush { Color = Color.FromArgb(180, 17, 94, 224) },
                Halo = new Pen(Color.FromArgb(220, 0, 0, 0), 2),
                Offset = new Offset(0, -24)
            });
        
        map.Layers.Add(_airportLayer);

        map.Layers.Add(CreateLayer(
            "Waypoints",
            WaypointMarkers,
            () => new SymbolStyle
            {
                SymbolType = SymbolType.Triangle,
                SymbolScale = 0.6,
                Fill = null,
                Outline = new Pen(Color.FromArgb(255, 255, 165, 0), 3)
            },
            () => new LabelStyle
            {
                LabelColumn = "Label",
                ForeColor = Color.Black,
                BackColor = new Brush { Color = Color.FromArgb(210, 255, 239, 186) },
                Halo = new Pen(Color.FromArgb(160, 255, 255, 255), 1.5),
                Offset = new Offset(0, -20)
            }));

        map.Layers.Add(CreateLayer(
            "VOR / DME",
            VorMarkers,
            () => new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 0.6,
                Fill = new Brush { Color = Color.FromArgb(255, 220, 38, 38) },
                Outline = null
            },
            () => new LabelStyle
            {
                LabelColumn = "Label",
                ForeColor = Color.White,
                BackColor = new Brush { Color = Color.FromArgb(200, 190, 24, 24) },
                Halo = new Pen(Color.FromArgb(220, 0, 0, 0), 2),
                Offset = new Offset(0, -24)
            }));

        map.Layers.Add(_routeLineLayer);
        map.Layers.Add(_routeMarkerLayer);

        map.Home = navigator => navigator.CenterOnAndZoomTo(DefaultCenter, DefaultRouteZoomResolution, 0, Easing.Linear);

        return map;
    }

    private void OnRouteStopsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateRouteLayers();
    }

    private void UpdateRouteLayers()
    {
        var markerFeatures = new List<IFeature>(_routeStops.Count);

        for (var index = 0; index < _routeStops.Count; index++)
        {
            var stop = _routeStops[index];
            stop.Order = index + 1;

            var displayLabel = stop.DisplayLabel;
            var feature = new PointFeature(stop.Position)
            {
                ["Label"] = displayLabel,
                ["Order"] = stop.Order,
                ["Name"] = stop.Marker.Name
            };

            if (!string.IsNullOrWhiteSpace(stop.Marker.Details))
            {
                feature["Details"] = stop.Marker.Details!;
            }

            feature.Styles.Clear();
            feature.Styles.Add(new SymbolStyle
            {
                SymbolType = SymbolType.Ellipse,
                SymbolScale = 1.2,
                Fill = new Brush { Color = Color.FromArgb(255, 255, 255, 255) },
                Outline = new Pen(Color.FromArgb(255, 17, 94, 224), 3)
            });

            feature.Styles.Add(new LabelStyle
            {
                LabelColumn = "Label",
                ForeColor = Color.White,
                BackColor = new Brush { Color = Color.FromArgb(220, 59, 130, 246) },
                Halo = new Pen(Color.FromArgb(200, 0, 0, 0), 2),
                Offset = new Offset(0, -22)
            });

            markerFeatures.Add(feature);
        }

        _routeMarkerLayer.Features = markerFeatures;
        _routeMarkerLayer.DataHasChanged();

        if (_routeStops.Count > 1)
        {
            var coordinates = _routeStops
                .Select(stop => new Coordinate(stop.Position.X, stop.Position.Y))
                .ToArray();

            var line = new LineString(coordinates);
            var lineFeature = line.ToFeature();
            lineFeature.Styles.Add(new VectorStyle
            {
                Line = new Pen(Color.FromArgb(255, 0, 0, 0), 8)
            });
            lineFeature.Styles.Add(new VectorStyle
            {
                Line = new Pen(Color.FromArgb(255, 96, 165, 250), 6)
            });

            _routeLineLayer.Features = new List<IFeature> { lineFeature };
        }
        else
        {
            _routeLineLayer.Features = new List<IFeature>();
        }

        _routeLineLayer.DataHasChanged();
    }

    private static MemoryLayer CreateLayer(
        string name,
        IReadOnlyList<NavMarker> markers,
        Func<SymbolStyle> symbolFactory,
        Func<LabelStyle?> labelFactory)
    {
        var features = new List<IFeature>(markers.Count);

        foreach (var marker in markers)
        {
            var feature = new PointFeature(ToPoint(marker.Latitude, marker.Longitude))
            {
                ["Name"] = marker.Name,
                ["Label"] = string.IsNullOrWhiteSpace(marker.Label) ? marker.Name : marker.Label,
                ["Category"] = name
            };

            if (!string.IsNullOrWhiteSpace(marker.Details))
            {
                feature["Details"] = marker.Details!;
            }

            feature.Styles.Clear();
            feature.Styles.Add(symbolFactory());

            var labelStyle = labelFactory();
            if (labelStyle is not null)
            {
                feature.Styles.Add(labelStyle);
            }

            features.Add(feature);
        }

        return new MemoryLayer(name)
        {
            Features = features,
            Style = null
        };
    }

    private static MPoint ToPoint(double latitude, double longitude)
    {
        var (x, y) = SphericalMercator.FromLonLat(longitude, latitude);
        return new MPoint(x, y);
    }

    private static readonly NavMarker[] AirportMarkers =
    {
        new("Dallas Fort Worth International Airport", "KDFW", 32.8972331, -97.0376947, "3200 East Airfield Drive, DFW Airport, TX 75261"),
        new("Fort Worth Meacham International Airport", "KFTW", 32.8197703, -97.36245, "251 American Concourse, Fort Worth, TX 76106"),
        new("Dallas Love Field Airport", "KDAL", 32.8459447, -96.8508767, "8008 Herb Kelleher Way, Dallas, TX 75235"),
        new("Perot Field Fort Worth Alliance Airport", "KAFW", 32.9903067, -97.3194294, "13300 Heritage Parkway, Fort Worth, TX 76177"),
        new("Addison Airport", "KADS", 32.96861, -96.83639, "16000 Dooley Road, Addison, TX 75001"),
        new("Arlington Municipal Airport", "KGKY", 32.663889, -97.094167, "2911 South Collins Street, Arlington, TX 76014"),
        new("Denton Enterprise Airport", "KDTO", 33.201961, -97.199098, "5000 Airport Road, Denton, TX 76207"),
        new("Grand Prairie Municipal Airport", "KGPM", 32.6987778, -97.0469167, "3116 S GT Southwest Pkwy, Grand Prairie, TX 75052"),
        new("McKinney National Airport", "KTKI", 33.1735, -96.5877, "1700 Airport Drive, McKinney, TX 75070"),
        new("Fort Worth NAS JRB (Carswell Field)", "KNFW", 32.7691861, -97.4415347, "1510 Chennault Avenue, Fort Worth, TX 76127")
    };

    private static readonly NavMarker[] WaypointMarkers =
    {
        new("DOJED Waypoint", "DOJED", 32.733647, -97.346231, null),
        new("ERPIF Waypoint", "ERPIF", 32.888928, -97.297703, null),
        new("IMELE Waypoint", "IMELE", 32.845575, -97.051097, null),
        new("GVINE Waypoint", "GVINE", 32.919722, -97.085833, null),
        new("NELYN Waypoint", "NELYN", 32.75, -96.916667, null),
        new("TORNN Waypoint", "TORNN", 32.683333, -97.0, null),
        new("KMART Waypoint", "KMART", 32.8, -97.45, null),
        new("LARRN Waypoint", "LARRN", 32.7, -97.2, null),
        new("MECHL Waypoint", "MECHL", 32.916667, -96.75, null),
        new("TREXX Waypoint", "TREXX", 32.683333, -97.083333, null)
    };

    private static readonly NavMarker[] VorMarkers =
    {
        new("MAVERICK (TTT) VOR/DME", "TTT", 32.869044, -97.073836, "VOR/DME 113.10 MHz"),
        new("COWBOY (CVE) VOR/DME", "CVE", 32.890308, -96.903964, "VOR/DME 116.20 MHz"),
        new("RANGER (FUZ) VORTAC", "FUZ", 32.8895, -97.179398, "VORTAC 115.70 MHz"),
        new("Fort Worth NAS JRB TACAN (NFW)", "NFW", 32.769167, -97.441667, "TACAN 108.70 MHz"),
        new("TEXOMA (URH) VOR/DME", "URH", 33.916667, -96.433333, "VOR/DME 114.30 MHz"),
        new("WACO (ACT) VORTAC", "ACT", 31.616667, -97.233333, "VORTAC 115.30 MHz"),
        new("CEDAR CREEK (CQY) VORTAC", "CQY", 32.283333, -96.133333, "VORTAC 114.80 MHz"),
        new("SULPHUR SPRINGS (SLR) VOR/DME", "SLR", 33.166667, -95.616667, "VOR/DME 109.00 MHz"),
        new("ARDMORE (ADM) VORTAC", "ADM", 34.3, -97.016667, "VORTAC 116.70 MHz"),
        new("GROESBECK (GNL) VOR/DME", "GNL", 31.533333, -96.533333, "VOR/DME 108.80 MHz")
    };

    public sealed record NavMarker(string Name, string Label, double Latitude, double Longitude, string? Details)
    {
        public override string ToString() => Label;
    }

    public sealed class RouteStop : ObservableObject
    {
        private int _order = 1;
        private bool _isDragging;

        public RouteStop(NavMarker marker, MPoint position)
        {
            Marker = marker;
            Position = position;
        }

        public NavMarker Marker { get; }

        public MPoint Position { get; }

        public int Order
        {
            get => _order;
            set => SetProperty(ref _order, value);
        }

        public bool IsDragging
        {
            get => _isDragging;
            set => SetProperty(ref _isDragging, value);
        }

        public string Label => Marker.Label;
        public string Name => Marker.Name;
        public string? Details => Marker.Details;
        public string DisplayLabel => $"{Order}. {Marker.Label}";
    }
}
