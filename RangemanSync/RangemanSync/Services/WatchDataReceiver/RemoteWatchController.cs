using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions.Contracts;
using RangemanSync.Services.Common;

namespace RangemanSync.Services.WatchDataReceiver
{
    internal class RemoteWatchController
    {
        private const int CommandDelay = 20;
        private readonly IDevice currentDevice;
        private readonly IWatchControllerUtilities watchControllerUtilities;
        private readonly ILoggerFactory loggerFactory;
        private ILogger<RemoteWatchController> logger;

        public RemoteWatchController(IDevice currentDevice, IWatchControllerUtilities watchControllerUtilities, ILoggerFactory loggerFactory)
        {
            this.currentDevice = currentDevice;
            this.watchControllerUtilities = watchControllerUtilities;
            this.watchControllerUtilities.Device = currentDevice;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger<RemoteWatchController>();
        }

        public async void SendMessageToDRSP(byte[] data)
        {
            // 00, 0F, 00, 10, 00, 00, 00, 20, 00, 00,
            var arrayToSend = new byte[] { 00, 0x0F, 00, 00, 00, 00, 00, 00, 00, 00 };
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), data);
        }

        public async void SendConfirmationToContinueTransmission()
        {
            var arrayToSend = new byte[] { 0x07, 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), arrayToSend);
        }

        public async void AskWatchToEndTransmission(byte categoryId)
        {
            var arrayToSend = new byte[] { 0x03, categoryId, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), arrayToSend);
        }

        /// <summary>
        /// Send command to the watch to download point memory or log entry
        /// </summary>
        /// <param name="address">result of GetLogAddress(i) or GetPointMemoryAddress</param>
        /// <param name="length">result of GetLogTotalLength(i) or GetPointMemoryTotalLength</param>
        public async Task SendPointMemoryOrLogDownload(int address, int length)
        {
            var b = (byte)0;
            var b2 = (byte)16;
            byte[] arrayToSend = { b, b2,
                (byte)(address & 255), (byte)((int)((uint)address >> 8) & 255),
                (byte)((int)((uint)address >> 16) & 255), (byte)((int)((uint)address >> 24) & 255),
                (byte)(length & 255), (byte)((int)((uint)length >> 8) & 255),
                (byte)((int)((uint)length >> 16) & 255), (byte)((int)((uint)length >> 24) & 255) };

            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), arrayToSend);
        }

        public async void SendHeaderClosingCommandsToWatch()
        {
            logger.LogDebug("-- Before  WriteCharacteristicValue 1");
            //TODO : Move it to else if (value.Item1 == Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic)) ?
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), new byte[] { 0x09, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            logger.LogDebug("-- After  WriteCharacteristicValue 1");

            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), new byte[] { 0x04, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            logger.LogDebug("-- After  WriteCharacteristicValue 2");
        }

        public async Task SubscribeToCharacteristicChanges(CasioConvoyAndCasioDataRequestObserver casioConvoyAndCasioDataRequestObserver)
        {
            var service = await currentDevice.GetServiceAsync(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid));
            
            var casioDataRequestSPCharacteristic = await service.GetCharacteristicAsync(
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic));

            casioDataRequestSPCharacteristic.ValueUpdated += (o, args) =>
            {
                casioConvoyAndCasioDataRequestObserver.OnNext(new Tuple<Guid, byte[]>(args.Characteristic.Id, 
                    args.Characteristic.Value));
            };

            await casioDataRequestSPCharacteristic.StartUpdatesAsync();

            var casioConvoyCharacteristic = await service.GetCharacteristicAsync(Guid.Parse(BLEConstants.CasioConvoyCharacteristic));

            casioConvoyCharacteristic.ValueUpdated += (o, args) =>
            {
                casioConvoyAndCasioDataRequestObserver.OnNext(new Tuple<Guid, byte[]>(args.Characteristic.Id,
                    args.Characteristic.Value));
            };

            await casioConvoyCharacteristic.StartUpdatesAsync();
        }

        public async Task SendDownloadLogCommandsToWatch()
        {
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioConvoyCharacteristic), new byte[] { 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

            await Task.Delay(CommandDelay);

            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioConvoyCharacteristic), new byte[] { 0x04, 0x01, 0x18, 0x00, 0x18, 0x00, 0x00, 0x00, 0x58, 0x02 });

            await Task.Delay(CommandDelay);

            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioConvoyCharacteristic), new byte[] { 0x02, 0x00, 0x00, 0x00, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

            await Task.Delay(CommandDelay);

            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioConvoyCharacteristic), new byte[] { 0x02, 0xF0, 0x00, 0x10, 0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF });

            await Task.Delay(CommandDelay);
        }

        public async Task SendDownloadHeaderCommandToWatch()
        {
            //8192 - sector size 0x2000
            //0x0F - category ID : header
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), new byte[] { 0x00, 0x0F, 0x00, 0x10, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00 });

            await Task.Delay(CommandDelay);
        }

        public async Task SendInitializationCommandsToWatch()
        {
            var charChangedObserver = new CharChangedObserver(loggerFactory);

            var service = await currentDevice.GetServiceAsync(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid));

            var casioAllFeaturesCharacteristic = await service.GetCharacteristicAsync(
                Guid.Parse(BLEConstants.CasioAllFeaturesCharacteristic));

            casioAllFeaturesCharacteristic.ValueUpdated += (o, args) =>
            {
               charChangedObserver.OnNext(new Tuple<Guid, byte[]>(args.Characteristic.Id,
                    args.Characteristic.Value));
            };

            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid), Guid.Parse(BLEConstants.CasioReadRequestForAllFeaturesCharacteristic), new byte[] { 0x11 });

            await Task.Delay(CommandDelay);

            await watchControllerUtilities.WriteDescriptorValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                                                  Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic),
                                                  Guid.Parse(BLEConstants.CCCDescriptor), new byte[] { 0x01, 0x00 });

            await Task.Delay(CommandDelay);

            await watchControllerUtilities.WriteDescriptorValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                                  Guid.Parse(BLEConstants.CasioConvoyCharacteristic),
                                  Guid.Parse(BLEConstants.CCCDescriptor), new byte[] { 0x01, 0x00 });

            await Task.Delay(CommandDelay);

            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), new byte[] { 0x00, 0x0E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

            await Task.Delay(CommandDelay);

            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioConvoyCharacteristic), new byte[] { 0x00, 0x00, 0x00 });
        }

    }

}
