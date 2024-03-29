﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RangemanSync.Services;
using RangemanSync.Services.WatchDataReceiver;
using RangemanSync.Services.WatchDataReceiver.DataExtractors.Data;
using System.Collections.ObjectModel;
using SharpGPX;
using RangemanSync.Services.Common;

namespace RangemanSync.ViewModels.Download
{
    public partial class MainPageViewModel : BaseViewModel
    {
        public IAsyncRelayCommand DownloadHeadersCommand { get; }
        public IAsyncRelayCommand SaveGPXCommand { get; }
        public IAsyncRelayCommand DisconnectCommand { get; set; }

        public ObservableCollection<LogHeaderViewModel> LogHeaderList { get; } = new ObservableCollection<LogHeaderViewModel>();
        public LogHeaderViewModel SelectedLogHeader { get; set; }

        public MainPageViewModel(ILoggerFactory loggerFactory, 
            BluetoothConnectorService bluetoothConnectorService, ISaveTextFileService saveFileService, 
            ProgressMessagesService progressMessagesService, IWatchControllerUtilities watchControllerUtilities)
        {
            this.logger = loggerFactory.CreateLogger<MainPageViewModel>();
            this.loggerFactory = loggerFactory;
            this.bluetoothConnectorService = bluetoothConnectorService;
            this.saveFileService = saveFileService;
            this.progressMessagesService = progressMessagesService;
            this.watchControllerUtilities = watchControllerUtilities;
            DownloadHeadersCommand = new AsyncRelayCommand(DownloadHeaders_Clicked);
            SaveGPXCommand = new AsyncRelayCommand(DownloadSaveGPXButton_Clicked);
            DisconnectCommand = new AsyncRelayCommand(DisconnectButton_Clicked);

            progressMessage = progressMessagesService.InitalStartMessage;
        }

        private ILogger<MainPageViewModel> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly BluetoothConnectorService bluetoothConnectorService;
        private readonly ISaveTextFileService saveFileService;
        private readonly ProgressMessagesService progressMessagesService;
        private readonly IWatchControllerUtilities watchControllerUtilities;
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

            SetProgressMessage(progressMessagesService.PleaseConnectWatch);

            await bluetoothConnectorService.FindAndConnectToWatch(SetProgressMessage,async (connection) =>
            {
                var logPointMemoryService = new LogPointMemoryExtractorService(connection, watchControllerUtilities, loggerFactory);
                logPointMemoryService.ProgressChanged += LogPointMemoryService_ProgressChanged;
                var headersTask = logPointMemoryService.GetHeaderDataAsync();
                var headers = await headersTask;

                LogHeaderList.Clear();

                if (headers != null && headers.Count > 0)
                {
                    headers.ForEach(h => LogHeaderList.Add(h.ToViewModel()));
                    await bluetoothConnectorService.DisconnectFromWatch(SetProgressMessage);
                }
                else
                {
                    logger.LogDebug("Headers downloading resulted 0 headers");
                    SetProgressMessage(progressMessagesService.ZeroHeaders);
                }

                logPointMemoryService.ProgressChanged -= LogPointMemoryService_ProgressChanged;

                DisconnectButtonIsVisible = false;
                WatchCommandButtonsAreVisible = true;

                lastHeaderDownloadTime = DateTime.Now;
                return true;
            },
            () =>
            {
                SetProgressMessage(progressMessagesService.WatchCommandSendingError);
                return Task.FromResult(true);
            },
            () =>
            { 
                DisconnectButtonIsVisible = true;
                WatchCommandButtonsAreVisible = false;
            });

        }

        private async Task DownloadSaveGPXButton_Clicked()
        {
            logger.LogInformation("--- MainPage - start DownloadSaveGPXButton_Clicked");

            if (SelectedLogHeader == null)
            {
                logger.LogDebug("DownloadSaveGPXButton_Clicked : One log header entry should be selected");
                SetProgressMessage(progressMessagesService.PleaseSelectLogHeader);
                return;
            }

            var timeElapsedSinceLastHeaderDownloadTime = DateTime.Now - lastHeaderDownloadTime;

            if (timeElapsedSinceLastHeaderDownloadTime.TotalMinutes > 30)
            {
                logger.LogDebug($"--- Old header data detected. Elapsed minutes = {timeElapsedSinceLastHeaderDownloadTime}");
                SetProgressMessage(progressMessagesService.ThirtyMinutesOldHeader);
                return;
            }

            SetProgressMessage(progressMessagesService.PleaseConnectWatch);

            saveGPXButtonCanBePressed = false;
            DisableOtherTabs();

            await bluetoothConnectorService.FindAndConnectToWatch(SetProgressMessage, async (connection) =>
            {
                logger.LogDebug("DownloadSaveGPXButton_Clicked : Before GetLogDataAsync");
                logger.LogDebug($"Selected ordinal number: {SelectedLogHeader.OrdinalNumber}");
                var logPointMemoryService = new LogPointMemoryExtractorService(connection, watchControllerUtilities, loggerFactory);
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
                    await bluetoothConnectorService.DisconnectFromWatch(SetProgressMessage);
                    SaveGPXFile(logDataEntries);
                }
                else
                {
                    ProgressMessage = progressMessagesService.TransmissionEndedWithoutReceivingAllData;
                    logger.LogDebug("-- Inside DownloadSaveAsGPXButton: logDataEntries is null");
                }

                DisconnectButtonIsVisible = false;
                WatchCommandButtonsAreVisible = true;

                return true;
            },
            async () =>
            {
                SetProgressMessage(progressMessagesService.WatchCommandSendingError);
                return true;
            },
            () => 
            { 
                DisconnectButtonIsVisible = true;
                WatchCommandButtonsAreVisible = false;
            });

            EnableOtherTabs();
            saveGPXButtonCanBePressed = true;
            //Save selected log header as GPX
        }

        private async Task DisconnectButton_Clicked()
        {
            await bluetoothConnectorService.DisconnectFromWatch(SetProgressMessage);
            DisconnectButtonIsVisible = false;
            WatchCommandButtonsAreVisible = true;
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
#if WINDOWS
            saveFileService.SaveFile(fileName, gpxString);
            SetProgressMessage(progressMessagesService.GPXSuccessfullySaved);
#else
            Preferences.Default.Set(Constants.PrefKeyGPX, gpxString);
            saveFileService.SaveFile(fileName);
#endif
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
