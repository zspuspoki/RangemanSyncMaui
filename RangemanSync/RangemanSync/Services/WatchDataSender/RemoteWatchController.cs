using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions.Contracts;
using RangemanSync.Services.Common;
using RangemanSync.Services.WatchDataReceiver;

namespace RangemanSync.Services.WatchDataSender
{
    internal class RemoteWatchController
    {
        private readonly IDevice currentDevice;
        private readonly IWatchControllerUtilities watchControllerUtilities;
        private ILogger<RemoteWatchController> logger;

        public RemoteWatchController(IDevice device, IWatchControllerUtilities watchControllerUtilities, 
            ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<RemoteWatchController>();
            this.currentDevice = device;
            this.watchControllerUtilities = watchControllerUtilities;
            this.watchControllerUtilities.Device = device;
        }

        public async Task SubscribeToCharacteristicChanges(WatchDataSenderObserver watchDataSenderObserver)
        {
            var service = await currentDevice.GetServiceAsync(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid));
            var casioConvoyCharacteristic = await service.GetCharacteristicAsync(Guid.Parse(BLEConstants.CasioConvoyCharacteristic));

            casioConvoyCharacteristic.ValueUpdated += (o, args) =>
            {
                watchDataSenderObserver.OnNext(new Tuple<Guid, byte[]>(args.Characteristic.Id,
                    args.Characteristic.Value));
            };

            await casioConvoyCharacteristic.StartUpdatesAsync();

            var casioDataRequestSPCharacteristic = await service.GetCharacteristicAsync(Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic));

            casioDataRequestSPCharacteristic.ValueUpdated += (o, args) =>
            {
                watchDataSenderObserver.OnNext(new Tuple<Guid, byte[]>(args.Characteristic.Id,
                    args.Characteristic.Value));
            };

            await casioDataRequestSPCharacteristic.StartUpdatesAsync();
        }

        public async Task SendInitCommandsAndWaitForCCCData(byte[] convoyData)
        {
            logger.LogDebug("--- SendInitCommandsAndWaitForCCCData - Before writing CasioDataRequestSPCharacteristic");
            await watchControllerUtilities.WriteDescriptorValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                                      Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic),
                                      Guid.Parse(BLEConstants.CCCDescriptor), new byte[] { 0x01, 0x00 });

            logger.LogDebug("--- SendInitCommandsAndWaitForCCCData - Before writing CasioConvoyCharacteristic");
            await watchControllerUtilities.WriteDescriptorValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                                  Guid.Parse(BLEConstants.CasioConvoyCharacteristic),
                                  Guid.Parse(BLEConstants.CCCDescriptor), new byte[] { 0x01, 0x00 });


            logger.LogDebug("--- SendInitCommandsAndWaitForCCCData - Before writing CasioDataRequestSPCharacteristic 2.");
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                                  Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), new byte[] { 0x00, 0x12, 0x00, 0x00 }); // Category = 18 - connection setup

        }


    }
}
