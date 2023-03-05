namespace RangemanSync.Services.DeviceLocation
{
    public interface ILocationService
    {
        Location Location { get; set; }

        void GetPhoneLocation();
    }
}
