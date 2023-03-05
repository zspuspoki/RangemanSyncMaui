using Plugin.BLE.Abstractions.Contracts;

namespace RangemanSync.Services.Common
{
    public class WatchControllerUtilities : IWatchControllerUtilities
    {
        private IDevice currentDevice;

        public WatchControllerUtilities()
        {
        }

        public IDevice Device { set => currentDevice = value; }

        public async Task<bool> WriteCharacteristicValue(Guid serviceGuid, Guid characteristicGuid,
            byte[] data)
        {
            if(currentDevice == null)
            {
                throw new NotSupportedException("Device cannot be null");
            }

            var service = await currentDevice.GetServiceAsync(serviceGuid);
            var characteristic = await service.GetCharacteristicAsync(characteristicGuid);
            return await characteristic.WriteAsync(data);
        }

        public async Task WriteDescriptorValue(Guid serviceGuid, Guid characteristicGuid,
            Guid descriptorGuid, byte[] data)
        {
            if(currentDevice == null)
            {
                throw new NotSupportedException("Device cannot be null");
            }

            var service = await currentDevice.GetServiceAsync(serviceGuid);
            var characteristic = await service.GetCharacteristicAsync(characteristicGuid);
            var descriptor = await characteristic.GetDescriptorAsync(descriptorGuid);
            await descriptor.WriteAsync(data);
        }
    }
}
