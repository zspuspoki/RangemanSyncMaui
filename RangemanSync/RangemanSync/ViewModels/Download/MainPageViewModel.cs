using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RangemanSync.Services;
using RangemanSync.Services.WatchDataReceiver;
using RangemanSync.Services.WatchDataReceiver.DataExtractors.Data;
using System.Collections.ObjectModel;
using SharpGPX;

namespace RangemanSync.ViewModels.Download
{
    public partial class MainPageViewModel : BaseViewModel
    {
        public IAsyncRelayCommand DownloadHeadersCommand { get; }
        public ObservableCollection<LogHeaderViewModel> LogHeaderList { get; } = new ObservableCollection<LogHeaderViewModel>();
        public LogHeaderViewModel SelectedLogHeader { get; set; }

        public MainPageViewModel(ILoggerFactory loggerFactory, 
            BluetoothConnectorService bluetoothConnectorService, ISaveGPXFileService saveGPXFileService)
        {
            this.logger = loggerFactory.CreateLogger<MainPageViewModel>();
            this.loggerFactory = loggerFactory;
            this.bluetoothConnectorService = bluetoothConnectorService;
            this.saveGPXFileService = saveGPXFileService;
            DownloadHeadersCommand = new AsyncRelayCommand(DownloadHeaders_Clicked);
        }

        private ILogger<MainPageViewModel> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly BluetoothConnectorService bluetoothConnectorService;
        private readonly ISaveGPXFileService saveGPXFileService;
        private DateTime lastHeaderDownloadTime;
        private bool saveGPXButtonCanBePressed = true;

        [ObservableProperty]
        string progressMessage;

        [ObservableProperty]
        bool disconnectButtonIsVisible;

        [ObservableProperty]
        bool watchCommandButtonsAreVisible = true;

        [ObservableProperty]
        string label;

        private async Task DownloadHeaders_Clicked()
        {
            logger.LogInformation("--- MainPage - start DownloadHeaders_Clicked");

            SetProgressMessage("Looking for Casio GPR-B1000 device. Please connect your watch.");

            await bluetoothConnectorService.FindAndConnectToWatch(SetProgressMessage,async (connection) =>
            {
                var logPointMemoryService = new LogPointMemoryExtractorService(connection, loggerFactory);
                logPointMemoryService.ProgressChanged += LogPointMemoryService_ProgressChanged;
                var headersTask = logPointMemoryService.GetHeaderDataAsync();
                var headers = await headersTask;

                LogHeaderList.Clear();

                if (headers != null && headers.Count > 0)
                {
                    headers.ForEach(h => LogHeaderList.Add(h.ToViewModel()));
                }
                else
                {
                    logger.LogDebug("Headers downloading resulted 0 headers");
                    SetProgressMessage("Headers downloading resulted 0 headers. Please make sure you have recorded routes on the watch. If yes, then please try again because the transmission has been terminated by the watch.");
                }

                logPointMemoryService.ProgressChanged -= LogPointMemoryService_ProgressChanged;

                DisconnectButtonIsVisible = false;
                lastHeaderDownloadTime = DateTime.Now;
                return true;
            },
            () =>
            {
                SetProgressMessage("An error occured during sending watch commands. Please try to connect again");
                return Task.FromResult(true);
            },
            () => DisconnectButtonIsVisible = true);

        }

        private async void DownloadSaveGPXButton_Clicked()
        {
            logger.LogInformation("--- MainPage - start DownloadSaveGPXButton_Clicked");

            if (SelectedLogHeader == null)
            {
                logger.LogDebug("DownloadSaveGPXButton_Clicked : One log header entry should be selected");
                SetProgressMessage("Please select a log header from the list or start downloading the list by using the download headers button if you haven't done it yet.");
                return;
            }

            var timeElapsedSinceLastHeaderDownloadTime = DateTime.Now - lastHeaderDownloadTime;

            if (timeElapsedSinceLastHeaderDownloadTime.TotalMinutes > 30)
            {
                logger.LogDebug($"--- Old header data detected. Elapsed minutes = {timeElapsedSinceLastHeaderDownloadTime}");
                SetProgressMessage("The header data is more than 30 minutes old. Please download the headers again by pressing the Download headers button.");
                return;
            }

            SetProgressMessage("Looking for Casio GPR-B1000 device. Please connect your watch.");

            saveGPXButtonCanBePressed = false;
            DisableOtherTabs();

            await bluetoothConnectorService.FindAndConnectToWatch(SetProgressMessage, async (connection) =>
            {
                logger.LogDebug("DownloadSaveGPXButton_Clicked : Before GetLogDataAsync");
                logger.LogDebug($"Selected ordinal number: {SelectedLogHeader.OrdinalNumber}");
                var logPointMemoryService = new LogPointMemoryExtractorService(connection, loggerFactory);
                logPointMemoryService.ProgressChanged += LogPointMemoryService_ProgressChanged;
                var selectedHeader = SelectedLogHeader;
                var logDataEntries = await logPointMemoryService.GetLogDataAsync(
                    selectedHeader.DataSize,
                    selectedHeader.DataCount,
                    selectedHeader.LogAddress,
                    selectedHeader.LogTotalLength);

                logPointMemoryService.ProgressChanged -= LogPointMemoryService_ProgressChanged;

                if (logDataEntries != null)
                {
                    logger.LogDebug("-- Inside DownloadSaveAsGPXButton: logDataEntries is not null! Calling SaveGPXFile()");
                    SaveGPXFile(logDataEntries);
                }
                else
                {
                    ProgressMessage = "The data downloading from the watch has been ended without receiving all of the data including the end transmission command. Please try again by pressing the download as GPX button again.";
                    logger.LogDebug("-- Inside DownloadSaveAsGPXButton: logDataEntries is null");
                }

                DisconnectButtonIsVisible = false;

                return true;
            },
            async () =>
            {
                SetProgressMessage("An error occured during sending watch commands. Please try to connect again");
                return true;
            },
            () => DisconnectButtonIsVisible = true);

            EnableOtherTabs();
            saveGPXButtonCanBePressed = true;
            //Save selected log header as GPX
        }

        private void DisableOtherTabs()
        {
            //TODO: Check implementation in MAUI
        }

        private void EnableOtherTabs()
        {
            //TODO: check implementation in MAUI
        }

        private void SaveGPXFile(List<LogData> logDataEntries)
        {
            GpxClass gpx = new GpxClass();

            gpx.Metadata.time = SelectedLogHeader.HeaderTime;
            gpx.Metadata.timeSpecified = true;
            gpx.Metadata.desc = "Track exported from Casio GPR-B1000 watch";

            gpx.Tracks.Add(new SharpGPX.GPX1_1.trkType());
            gpx.Tracks[0].trkseg.Add(new SharpGPX.GPX1_1.trksegType());

            foreach (var logEntry in logDataEntries)
            {
                var wpt = new SharpGPX.GPX1_1.wptType
                {
                    lat = (decimal)logEntry.Latitude,
                    lon = (decimal)logEntry.Longitude,   // ele tag : pressure -> elevation conversion ?
                    time = logEntry.Date,
                    timeSpecified = true,
                };

                gpx.Tracks[0].trkseg[0].trkpt.Add(wpt);
            }

            var headerTime = SelectedLogHeader.HeaderTime;
            var fileName = $"GPR-B1000-Route-{headerTime.Year}-{headerTime.Month}-{headerTime.Day}-2.gpx";

            var gpxString = gpx.ToXml();
            Preferences.Default.Set(Constants.PrefKeyGPX, gpxString);
            saveGPXFileService.SaveGPXFile(fileName);
        }


        private void LogPointMemoryService_ProgressChanged(object sender, DataReceiverProgressEventArgs e)
        {
            SetProgressMessage(e.Text);
        }

        private void SetProgressMessage(string message)
        {
            ProgressMessage = message;
        }


    }
}
