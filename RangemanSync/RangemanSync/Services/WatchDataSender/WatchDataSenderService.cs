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

            var remoteWatchController = new RemoteWatchController(currentDevice,watchControllerUtilities, loggerFactory);

            await remoteWatchController.SendInitCommandsAndWaitForCCCData(new byte[] { 00, 00, 00 });

            FireProgressEvent(ref progressPercent, 8, "Sent init commands and waited for CCC data");

            await remoteWatchController.SendConvoyConnectionParameters();

            FireProgressEvent(ref progressPercent, 8, "Sent convoy connection parameters");

            var categoriesToSend = new CategoryToSend[] {
                new CategoryToSend(0x16, data),
                new CategoryToSend(0x15, header) };

            foreach (var category in categoriesToSend)
            {
                var connectionParameters = await remoteWatchController.SendCategoryAndWaitForConnectionParams(category.CategoryId);  // Category id = 22 - route

                FireProgressEvent(ref progressPercent, 8, "Sent category and waited for connection params");

                await remoteWatchController.SendConnectionSettingsBasedOnParams(connectionParameters, data.Length, category.CategoryId);

                FireProgressEvent(ref progressPercent, 8, "Sent connection settings based on params");

                BufferedConvoySender bufferedConvoySender = new BufferedConvoySender(currentDevice, watchControllerUtilities, category.Data, loggerFactory);
                await bufferedConvoySender.Send();

                FireProgressEvent(ref progressPercent, 8, $"Finished using buffered convoy sender. Category = {category.CategoryId}");

                await remoteWatchController.CloseCurrentCategoryAndWaitForResponse(category.CategoryId);

                FireProgressEvent(ref progressPercent, 8, "Closed current category and waited for response");
            }

            await remoteWatchController.WriteFinalClosingData();
            FireProgressEvent(ref progressPercent, 8, "Finished writing final closing data");

            await remoteWatchController.WriteFinalClosingData2();

            progressPercent = 100;
            FireProgressEvent(ref progressPercent, 0, "Finished sending data");
        }

        private void FireProgressEvent(ref int percentage, int increment, string text)
        {
            if (ProgressChanged != null)
            {
                var eventArgs = new DataSenderProgressEventArgs { PercentageText = $"{percentage}%", Text = text, PercentageNumber = percentage };
                ProgressChanged(this, eventArgs);

                percentage += increment;
            }
        }
    }
}
