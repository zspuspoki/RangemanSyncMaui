using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RangemanSync.Services;
using RangemanSync.Services.WatchDataReceiver;
using System.Collections.ObjectModel;

namespace RangemanSync.ViewModels.Download
{
    public partial class MainPageViewModel : BaseViewModel
    {
        public IAsyncRelayCommand DownloadHeadersCommand { get; }
        public IAsyncRelayCommand DisconnectCommand { get; set; }

        public ObservableCollection<LogHeaderViewModel> LogHeaderList { get; } = new ObservableCollection<LogHeaderViewModel>();
        public LogHeaderViewModel SelectedLogHeader { get; set; }

        public MainPageViewModel(ILoggerFactory loggerFactory, 
            BluetoothConnectorService bluetoothConnectorService, ISaveTextFileService saveFileService, ProgressMessagesService progressMessagesService)
        {
            this.logger = loggerFactory.CreateLogger<MainPageViewModel>();
            this.loggerFactory = loggerFactory;
            this.bluetoothConnectorService = bluetoothConnectorService;
            this.saveFileService = saveFileService;
            this.progressMessagesService = progressMessagesService;
            DownloadHeadersCommand = new AsyncRelayCommand(DownloadHeaders_Clicked);
            DisconnectCommand = new AsyncRelayCommand(DisconnectButton_Clicked);

            progressMessage = progressMessagesService.InitalStartMessage;
        }

        private ILogger<MainPageViewModel> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly BluetoothConnectorService bluetoothConnectorService;
        private readonly ISaveTextFileService saveFileService;
        private readonly ProgressMessagesService progressMessagesService;
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
                var logPointMemoryService = new LogPointMemoryExtractorService(connection, loggerFactory);
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
