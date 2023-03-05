using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using RangemanSync.Services;
using RangemanSync.Services.Common;
using RangemanSync.Services.DeviceLocation;
using RangemanSync.Services.WatchDataSender;
using System.Windows.Input;

namespace RangemanSync.ViewModels.Map
{
    public partial class MapPageViewModel : BaseViewModel
    {
        private bool showCalculatedDistance = false;
        private bool addressPanelIsVisible = false;

        [ObservableProperty]
        private bool progressBarIsVisible;

        [ObservableProperty]
        private string progressBarPercentageMessage;

        [ObservableProperty]
        private string progressMessage;

        [ObservableProperty]
        private double progressBarPercentageNumber;

        [ObservableProperty]
        private RowDefinitionCollection gridViewRows;

        [ObservableProperty]
        private bool watchCommandButtonsAreVisible = true;

        [ObservableProperty]
        private bool disconnectButtonIsVisible = false;

        private NodesViewModel nodesViewModel;
        private readonly IMapPageView mapPageView;
        private readonly BluetoothConnectorService bluetoothConnectorService;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILocationService locationService;
        private readonly IWatchControllerUtilities watchControllerUtilities;

        private ILogger<MapPageViewModel> logger;

        private bool sendButtonCanbePressed = true;
        private bool deleteButtonCanbePressed = true;
        private bool selectButtonCanbePressed = true;
        private bool addressButtonCanbePressed = true;
        private bool disconnectButtonCanbePressed = true;
        private bool hasValidLicense = true;

        private ICommand sendCommand;
        private ICommand deleteCommand;
        private ICommand selectCommand;
        private ICommand addressCommand;
        private ICommand disconnectCommand;

        public MapPageViewModel(NodesViewModel nodesViewModel,
            IMapPageView mapPageView,
            BluetoothConnectorService bluetoothConnectorService,
            ILoggerFactory loggerFactory, ILocationService locationService,
            IWatchControllerUtilities watchControllerUtilities)
        {
            this.logger = loggerFactory.CreateLogger<MapPageViewModel>();

            logger.LogInformation("Inside Map page VM ctor");

            this.nodesViewModel = nodesViewModel;
            this.mapPageView = mapPageView;
            this.bluetoothConnectorService = bluetoothConnectorService;
            this.loggerFactory = loggerFactory;
            this.locationService = locationService;
            this.watchControllerUtilities = watchControllerUtilities;
            gridViewRows = new RowDefinitionCollection
            {
                new RowDefinition { Height = new GridLength(2, GridUnitType.Star) },
                new RowDefinition { Height = 0 }
            };
        }

        public void ShowDistanceFromCurrentPosition(double longitude, double latitude)
        {
            if (!ShowCalculatedDistances)
            {
                return;
            }

            try
            {
                var location = locationService.Location;
                if (location != null)
                {
                    var distance = Location.CalculateDistance(location.Latitude, location.Longitude,
                        latitude, longitude, DistanceUnits.Kilometers);

                    ProgressMessage = $"Distance from the current position: {distance.ToString("N3")} km";
                }
                else
                {
                    ProgressMessage = "The phone's current position cannot be determined. Do you have a working location service ?";
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                ProgressMessage = "An unexpexcted error occured during calculating the distance. ( current point - selected point)";
            }
        }

        public void UpdateMapToUseMbTilesFile()
        {
            mapPageView.UpdateMapToUseMbTilesFile();
        }

        public void UpdateMapToUseWebBasedMbTiles()
        {
            mapPageView.UpdateMapToUseWebBasedMbTiles();
        }

        public bool ToggleAddressPanelVisibility()
        {
            if (!addressPanelIsVisible)
            {
                GridViewRows = new RowDefinitionCollection
                {
                    new RowDefinition { Height = new GridLength(2, GridUnitType.Star) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
                };
            }
            else
            {
                GridViewRows = new RowDefinitionCollection
                {
                    new RowDefinition { Height = new GridLength(2, GridUnitType.Star) },
                    new RowDefinition { Height = 0 }
                };
            }

            addressPanelIsVisible = !addressPanelIsVisible;

            return addressPanelIsVisible;
        }

        #region Button commands
        #region Button click handlers
        private async void SendButton_Clicked()
        {
            logger.LogInformation("--- MapPage - start SendButton_Clicked");

            if (!hasValidLicense)
            {
                ProgressMessage = "Invalid license detected : the sending is not allowed.";
                return;
            }

            if (!NodesViewModel.HasRoute())
            {
                await mapPageView.DisplayAlert("Alert", "Please create a route before pressing Send.", "OK");
                return;
            }

            sendButtonCanbePressed = false;

            ProgressMessage = "Looking for Casio GPR-B1000 device. Please connect your watch.";
            await bluetoothConnectorService.FindAndConnectToWatch((message) => ProgressMessage = message,
                async (connection) =>
                {
                    logger.LogDebug("Map tab - Device Connection was successful");
                    ProgressMessage = "Connected to GPR-B1000 watch.";

                    MapPageDataConverter mapPageDataConverter = new MapPageDataConverter(NodesViewModel, loggerFactory);

                    var watchDataSenderService = new WatchDataSenderService(connection, watchControllerUtilities, mapPageDataConverter.GetDataByteArray(),
                        mapPageDataConverter.GetHeaderByteArray(), loggerFactory);

                    watchDataSenderService.ProgressChanged += WatchDataSenderService_ProgressChanged;
                    await watchDataSenderService.SendRoute();

                    logger.LogDebug("Map tab - after awaiting SendRoute()");
                    DisconnectButtonIsVisible = false;
                    ProgressBarIsVisible = false;

                    return true;
                },
                async () =>
                {
                    ProgressMessage = "An error occured during sending watch commands. Please try to connect again";
                    return true;
                },
                () => DisconnectButtonIsVisible = true);

            sendButtonCanbePressed = true;
        }

        private void DeleteNodeButton_Clicked()
        {
            try
            {
                deleteButtonCanbePressed = false;
                NodesViewModel.DeleteSelectedNode();
                mapPageView.RemoveSelectedPin();
                mapPageView.AddLinesBetweenPinsAsLayer();
                ProgressMessage = "Successfully deleted node.";
                deleteButtonCanbePressed = true;
            }
            catch (InvalidOperationException ex)
            {
                logger.LogError(ex, "Error occured during deleting a node.");
                ProgressMessage = ex.Message;
            }
        }

        private void SelectNodeButton_Clicked()
        {
            selectButtonCanbePressed = false;
            NodesViewModel.ClickOnSelectNode();
            selectButtonCanbePressed = true;
        }

        private async void DisconnectButton_Clicked()
        {
            disconnectButtonCanbePressed = false;
            await bluetoothConnectorService.DisconnectFromWatch((m) => ProgressMessage = m);
            DisconnectButtonIsVisible = false;
            disconnectButtonCanbePressed = true;
            ProgressBarIsVisible = false;
        }
        #endregion

        #region Helper methods
        private void WatchDataSenderService_ProgressChanged(object sender, DataSenderProgressEventArgs e)
        {
            ProgressBarIsVisible = true;
            ProgressMessage = e.Text;
            ProgressBarPercentageMessage = e.PercentageText;
            ProgressBarPercentageNumber = e.PercentageNumber;

            logger.LogDebug($"Current progress bar percentage number: {ProgressBarPercentageNumber}");
        }

        #endregion

        #endregion

        #region Properties

        public NodesViewModel NodesViewModel { get => nodesViewModel; }


        #region Button Commands
        public ICommand SendCommand
        {
            get
            {
                if (sendCommand == null)
                {
                    sendCommand = new Command((o) => SendButton_Clicked(), (o) => sendButtonCanbePressed);
                }

                return sendCommand;
            }
        }

        public ICommand DeleteCommand
        {
            get
            {
                if (deleteCommand == null)
                {
                    deleteCommand = new Command((o) => DeleteNodeButton_Clicked(), (o) => deleteButtonCanbePressed);
                }

                return deleteCommand;
            }
        }

        public ICommand SelectCommand
        {
            get
            {
                if (selectCommand == null)
                {
                    selectCommand = new Command((o) => SelectNodeButton_Clicked(), (o) => selectButtonCanbePressed);
                }

                return selectCommand;
            }
        }

        public ICommand DisconnectCommand
        {
            get
            {
                if (disconnectCommand == null)
                {
                    disconnectCommand = new Command((o) => DisconnectButton_Clicked(), (o) => disconnectButtonCanbePressed);
                }

                return disconnectCommand;
            }
        }

        #endregion

        public bool ShowCalculatedDistances
        {
            get
            {
                return showCalculatedDistance;
            }
            set
            {
                showCalculatedDistance = value;
            }
        }
        #endregion
    }
}
