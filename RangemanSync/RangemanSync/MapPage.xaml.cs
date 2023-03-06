using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.Tiling;
using Mapsui.UI.Maui;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using RangemanSync.ViewModels.Map;
using RangemanSync.Services.DeviceLocation;
using Point = NetTopologySuite.Geometries.Point;
using NetTopologySuite.Features;
using Mapsui.Nts;

namespace RangemanSync;

public partial class MapPage : ContentPage, IMapPageView
{
    private const string LinesLayerName = "LinesBetweenPins";
    private readonly ILogger<MapPage> logger;
    private readonly ILocationService locationService;

    public MapPage(ILogger<MapPage> logger, ILocationService locationService)
    {
        this.logger = logger;
        this.locationService = locationService;
        locationService.GetPhoneLocation();
        InitializeComponent();

        InitProgressLabel();
    }

    private void InitProgressLabel()
    {
        lblProgress.GestureRecognizers.Add(new TapGestureRecognizer
        {
            //TODO: Move this command to the viewmodel
            Command = new Command(() =>
            {
                ViewModel.ProgressMessage = "";
            })
        });
    }

    private async Task<bool> InitializeMap(bool forceMapUpdate = false)
    {
        bool hasTileLayer = false;

        if (mapView.Map != null)
        {
            foreach(var layer in mapView.Map.Layers)
            {
                if (layer is TileLayer) hasTileLayer = true;
            }
        }

        if (!forceMapUpdate && hasTileLayer)
        {
            return false;
        }

        var map = new Mapsui.Map
        {
            CRS = "EPSG:3857",
        };

        var tileLayer = OpenStreetMap.CreateTileLayer();

        map.Layers.Add(tileLayer);
        map.Widgets.Add(new Mapsui.Widgets.ScaleBar.ScaleBarWidget(map)
        {
            TextAlignment = Mapsui.Widgets.Alignment.Center,
            HorizontalAlignment = Mapsui.Widgets.HorizontalAlignment.Left,
            VerticalAlignment = Mapsui.Widgets.VerticalAlignment.Bottom
        });

        var location = locationService.Location;

        if (location != null)
        {
            var smc = SphericalMercator.FromLonLat(location.Longitude, location.Latitude);

            var mapResolutions = map.Resolutions; 
            map.Home = n => n.NavigateTo(new Mapsui.MPoint(smc.x, smc.y), map.Resolutions[17]);

            mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(location.Latitude, location.Longitude), true);
        }

        mapView.Map = map;
        return true;
    }



    protected override async void OnAppearing()
    {
        base.OnAppearing();

        var mapInitialized = await InitializeMap();

        if (!mapInitialized)
        {
            var location = locationService.Location;

            if (location != null)
            {
                Console.WriteLine($"Latitude: {location.Latitude}, Longitude: {location.Longitude}, Altitude: {location.Altitude}");
            }

            mapView.MyLocationLayer.UpdateMyLocation(new Mapsui.UI.Maui.Position(location.Latitude, location.Longitude), true);
        }
    }

    #region Pin related methods
    private void mapView_MapClicked(object sender, MapClickedEventArgs e)
    {
        var pinTitle = AddNodeToViewModelAndGetPinTitle(e.Point.Longitude, e.Point.Latitude);

        if (pinTitle == null)
        {
            return;
        }

        ShowPinOnMap(pinTitle, e.Point);
    }

    /// <summary>
    /// Creates a layer with lines between the pin points
    /// </summary>
    /// <param name="name"></param>
    /// <param name="geoWaypoints"></param>
    /// <returns></returns>
    public void AddLinesBetweenPinsAsLayer()
    {
        mapView.Map.Layers.Remove((layer) => layer.Name == LinesLayerName);

        var points = new List<Coordinate>();
        foreach (var wp in ViewModel.NodesViewModel.GetLineConnectableCoordinatesFromStartToGoal())
        {
            if (wp.HasValidCoordinates)
            {
                var coordinates = SphericalMercator.FromLonLat(wp.Longitude, wp.Latitude);
                var coordinate = new Coordinate(coordinates.x, coordinates.y);
                points.Add(coordinate);
            }
        }

        GeometryFeature lineStringFeature = new GeometryFeature()
        {
            Geometry = new LineString(points.ToArray())
        };

        IStyle linestringStyle = new VectorStyle()
        {
            Fill = null,
            Outline = null,
            Line = { Color = Mapsui.Styles.Color.FromString("Blue"), Width = 4 }
        };

        var linesLayer = new MemoryLayer
        {
            Name = LinesLayerName,
            Style = linestringStyle,
            Features = new List<Mapsui.IFeature> { lineStringFeature }
        };


        mapView.Map.Layers.Add(linesLayer);
    }

    private string AddNodeToViewModelAndGetPinTitle(double longitude, double latitude)
    {
        ViewModel.ShowDistanceFromCurrentPosition(longitude, latitude);
        var pinTitle = ViewModel.NodesViewModel.AddNodeToMap(longitude, latitude);
        return pinTitle;
    }

    private void ShowPinOnMap(string pinTitle, Mapsui.UI.Maui.Position p, bool setTitleImmediately = false)
    {
        var pin = new Pin(mapView)
        {
            Label = "unset",
            Position = p,
            RotateWithMap = false
        };

        pin.Callout.Type = CalloutType.Single;

        if (setTitleImmediately)
        {
            pin.Callout.Title = pinTitle;
        }

        pin.Callout.CalloutClicked += (s, e) =>
        {
            logger.LogDebug($"Map page: entering callout clicked");
            if (e.Callout.Title == "unset")
            {
                logger.LogDebug($"Map page: callout clicked. Number of taps: {e.NumOfTaps}");
                pin.Callout.Title = pinTitle;
                e.Handled = true;
                e.Callout.Type = CalloutType.Single;
            }
            else
            {
                if (BindingContext is MapPageViewModel mapPageViewModel)
                {
                    logger.LogDebug("Map: Callout click - other");
                    e.Handled = true;
                    var pinTitle = e.Callout.Title;
                    mapPageViewModel.NodesViewModel.SelectNodeForDeletion(pinTitle, e.Point.Longitude, e.Point.Latitude);
                    mapPageViewModel.ProgressMessage = $"Selected node: {pinTitle} Please use the delete button to delete it.";
                    mapView.SelectedPin = e.Callout.Pin;
                }
            }
        };

        mapView.Pins.Add(pin);
        pin.ShowCallout();
        AddLinesBetweenPinsAsLayer();
    }

    public void PlaceOnMapClicked(Mapsui.UI.Maui.Position p)
    {
        var pinTitle = AddNodeToViewModelAndGetPinTitle(p.Longitude, p.Latitude);

        if (pinTitle == null)
        {
            return;
        }

        ShowPinOnMap(pinTitle, p, true);
    }

    public void RemoveSelectedPin()
    {
        mapView.Pins.Remove(mapView.SelectedPin);
    }
    #endregion

    #region MBTiles related methods


    #endregion

    Task IMapPageView.DisplayAlert(string title, string message, string button)
    {
        return DisplayAlert(title, message, button);
    }

    public void DisplayProgressMessage(string message)
    {
        ViewModel.ProgressMessage = message;
    }

    public void UpdateMapToUseMbTilesFile()
    {
        throw new NotImplementedException();
    }

    public void UpdateMapToUseWebBasedMbTiles()
    {
        throw new NotImplementedException();
    }

    public void ShowOnMap(Mapsui.UI.Maui.Position p)
    {
        throw new NotImplementedException();
    }

    public MapPageViewModel ViewModel
    {
        get
        {
            var vm = BindingContext as MapPageViewModel;
            return vm;
        }
    }

}