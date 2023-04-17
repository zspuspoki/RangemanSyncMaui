using Plugin.BLE.Abstractions.Contracts;

namespace RangemanSync.Services.Common
{
    public class WatchControllerUtilities : IWatchControllerUtilities
    {
        private const int CommandDelay = 20;
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
            await Task.Delay(CommandDelay);

            var characteristic = await service.GetCharacteristicAsync(characteristicGuid);
            await Task.Delay(CommandDelay);

            var result = await characteristic.WriteAsync(data);
            await Task.Delay(CommandDelay);

            return result;
        }

        public async Task WriteDescriptorValue(Guid serviceGuid, Guid characteristicGuid,
            Guid descriptorGuid, byte[] data)
        {
            if(currentDevice == null)
            {
                throw new NotSupportedException("Device cannot be null");
            }

            var service = await currentDevice.GetServiceAsync(serviceGuid);
            await Task.Delay(CommandDelay);

            var characteristic = await service.GetCharacteristicAsync(characteristicGuid);
            await Task.Delay(CommandDelay);

            var descriptor = await characteristic.GetDescriptorAsync(descriptorGuid);
            await Task.Delay(CommandDelay);

            await descriptor.WriteAsync(data);
            await Task.Delay(CommandDelay);
        }
    }
}
