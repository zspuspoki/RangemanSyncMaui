using Plugin.BLE.Abstractions.Contracts;

namespace RangemanSync.Services.Common
{
    public interface IWatchControllerUtilities
    {
        Task<bool> WriteCharacteristicValue(Guid serviceGuid, Guid characteristicGuid, byte[] data);
        Task WriteDescriptorValue(Guid serviceGuid, Guid characteristicGuid,
            Guid descriptorGuid, byte[] data);

        IDevice Device { set; } 
    }
}
