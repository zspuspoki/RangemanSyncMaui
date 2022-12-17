namespace RangemanSync.Platforms.Android
{
    public class BluetoothLEPermissions : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions
        {
            get
            {
                return new List<(string androidPermission, bool isRuntime)>
            {

                (global::Android.Manifest.Permission.Bluetooth, true),
                (global::Android.Manifest.Permission.BluetoothAdmin, true),
                (global::Android.Manifest.Permission.BluetoothScan, true),
                (global::Android.Manifest.Permission.BluetoothConnect, true),
                (global::Android.Manifest.Permission.AccessFineLocation, true),
                (global::Android.Manifest.Permission.AccessCoarseLocation, true),
                //(Android.Manifest.Permission.AccessBackgroundLocation, true),

            }.ToArray();
            }
        }
    }
}
