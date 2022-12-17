using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using RangemanSync.Services;
using System.Collections.ObjectModel;

namespace RangemanSync.ViewModels
{
    public partial class MainPageViewModel : BaseViewModel
    {
        public IAsyncRelayCommand DownloadHeadersCommand { get; }
        public ObservableCollection<LogHeaderViewModel> LogHeaderList { get; } = new ObservableCollection<LogHeaderViewModel>();
        public LogHeaderViewModel SelectedLogHeader { get; set; }

        public MainPageViewModel(ILoggerFactory loggerFactory, BluetoothConnectorService bluetoothConnectorService)
        {
            this.logger = loggerFactory.CreateLogger<MainPageViewModel>();
            this.loggerFactory = loggerFactory;
            this.bluetoothConnectorService = bluetoothConnectorService;

            DownloadHeadersCommand = new AsyncRelayCommand(DownloadHeaders_Clicked);
        }

        private ILogger<MainPageViewModel> logger;
        private readonly ILoggerFactory loggerFactory;
        private readonly BluetoothConnectorService bluetoothConnectorService;

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

            await bluetoothConnectorService.FindAndConnectToWatch(SetProgressMessage, (connection) =>
            {
                //var logPointMemoryService = new LogPointMemoryExtractorService(connection, loggerFactory);
                //logPointMemoryService.ProgressChanged += LogPointMemoryService_ProgressChanged;
                //var headersTask = logPointMemoryService.GetHeaderDataAsync();
                //var headers = await headersTask;

                LogHeaderList.Clear();

                //if (headers != null && headers.Count > 0)
                //{
                //    headers.ForEach(h => LogHeaderList.Add(h.ToViewModel()));
                //}
                //else
                //{
                //    logger.LogDebug("Headers downloading resulted 0 headers");
                //    SetProgressMessage("Headers downloading resulted 0 headers. Please make sure you have recorded routes on the watch. If yes, then please try again because the transmission has been terminated by the watch.");
                //}

                //logPointMemoryService.ProgressChanged -= LogPointMemoryService_ProgressChanged;

                //DisconnectButtonIsVisible = false;
                //lastHeaderDownloadTime = DateTime.Now;
                return Task.FromResult(true);
            },
            () =>
            {
                SetProgressMessage("An error occured during sending watch commands. Please try to connect again");
                return Task.FromResult(true);
            },
            () => DisconnectButtonIsVisible = true);

        }

        private void SetProgressMessage(string message)
        {
            ProgressMessage = message;
        }


    }
}
