using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions.Contracts;
using RangemanSync.Services.Common;

namespace RangemanSync.Services.WatchDataSender
{
    internal class WatchDataSenderService
    {
        public event EventHandler<DataSenderProgressEventArgs> ProgressChanged;

        private readonly IDevice currentDevice;
        private readonly IWatchControllerUtilities watchControllerUtilities;
        private readonly byte[] data;
        private readonly byte[] header;
        private readonly ILoggerFactory loggerFactory;
        private ILogger<WatchDataSenderService> logger;

        public WatchDataSenderService(IDevice currentDevice, IWatchControllerUtilities watchControllerUtilities,
            byte[] data, byte[] header, ILoggerFactory loggerFactory)
        {
            this.currentDevice = currentDevice;
            this.watchControllerUtilities = watchControllerUtilities;
            this.data = data;
            this.header = header;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger<WatchDataSenderService>();
        }

        public async Task SendRoute()
        {
            var progressPercent = 8;

            logger.LogInformation("--- Starting SendRoute()");
            logger.LogDebug("Test");

            var remoteWatchController = new RemoteWatchController(currentDevice,watchControllerUtilities, loggerFactory);
            var watchDataSenderObserver = new WatchDataSenderObserver(currentDevice, loggerFactory, watchControllerUtilities, data, header);
            watchDataSenderObserver.ProgressChanged += ProgressChanged;

            await remoteWatchController.SubscribeToCharacteristicChanges(watchDataSenderObserver);

            await remoteWatchController.SendInitCommandsAndWaitForCCCData(new byte[] { 00, 00, 00 });
        }

       
    }
}
