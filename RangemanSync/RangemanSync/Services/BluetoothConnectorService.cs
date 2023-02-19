﻿using Microsoft.Extensions.Logging;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;

namespace RangemanSync.Services
{
    public class BluetoothConnectorService
    {
        private const string WatchDeviceName = "CASIO GPR-B1000";
        
        private readonly ILogger<BluetoothConnectorService> logger;
        private CancellationTokenSource scanCancellationTokenSource = null;
        private IDevice currentDevice;
        private IBluetoothLE bluetoothLE;
        private IAdapter adapter;

        public BluetoothConnectorService(ILogger<BluetoothConnectorService> logger)
        {
            this.logger = logger;
            bluetoothLE = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.ScanTimeout = 70000;
            adapter.DeviceDiscovered += Adapter_DeviceDiscovered;
        }

        private void Adapter_DeviceDiscovered(object sender, DeviceEventArgs e)
        {
            currentDevice = e.Device;

            if(scanCancellationTokenSource != null)
            {
                scanCancellationTokenSource.Cancel();
            }
        }

        public async Task FindAndConnectToWatch(Action<string> progressMessageMethod,
               Func<IDevice, Task<bool>> successfullyConnectedMethod,
               Func<Task<bool>> watchCommandExecutionFailed = null,
               Action beforeStartScanningMethod = null)
        {
            if (progressMessageMethod is null)
            {
                throw new ArgumentNullException(nameof(progressMessageMethod));
            }

            if (successfullyConnectedMethod is null)
            {
                throw new ArgumentNullException(nameof(successfullyConnectedMethod));
            }

            if (beforeStartScanningMethod != null)
            {
                beforeStartScanningMethod();
            }

            scanCancellationTokenSource = new CancellationTokenSource();
            await adapter.StartScanningForDevicesAsync(scanFilterOptions: null, 
                (device) => device.Name != null && device.Name.Contains(WatchDeviceName), 
                allowDuplicatesKey: false, scanCancellationTokenSource.Token);

            if(currentDevice != null)
            {
                progressMessageMethod("Found Casio device. Trying to connect ...");

                try
                {
                    await adapter.ConnectToDeviceAsync(currentDevice);

                    progressMessageMethod("Successfully connected to the watch.");

                    await successfullyConnectedMethod(currentDevice);
                }
                catch(DeviceConnectionException e)
                {
                    logger.LogDebug("Failed to connect to watch");
                }
            }

        }

        public async Task DisconnectFromWatch(Action<string> progressMessageMethod)
        {
            try
            {
                if (progressMessageMethod is null)
                {
                    throw new ArgumentNullException(nameof(progressMessageMethod));
                }

                logger.LogInformation("Started DisconnectFromWatch");

                if (scanCancellationTokenSource != null && !scanCancellationTokenSource.IsCancellationRequested)
                {
                    scanCancellationTokenSource.Cancel();
                    progressMessageMethod("Bluetooth: GPR-B1000 device scanning successfully aborted.");
                }

                foreach(var device in adapter.ConnectedDevices)
                {
                    await adapter.DisconnectDeviceAsync(device);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occured during disconnecting the watch.");
                progressMessageMethod("An unexpected error occured during disconnecting the watch.");
            }
        }

#if ANDROID
            #region BluetoothPermissions
        public async Task<PermissionStatus> CheckBluetoothPermissions()
        {
            PermissionStatus status = PermissionStatus.Unknown;
            try
            {
                status = await Permissions.CheckStatusAsync<Platforms.Android.BluetoothLEPermissions>();
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Unable to check Bluetooth LE permissions: {ex.Message}.");
                await Shell.Current.DisplayAlert($"Unable to check Bluetooth LE permissions", $"{ex.Message}.", "OK");
            }
            return status;
        }

        public async Task<PermissionStatus> RequestBluetoothPermissions()
        {
            PermissionStatus status = PermissionStatus.Unknown;
            try
            {
                status = await Permissions.RequestAsync<Platforms.Android.BluetoothLEPermissions>();
            }
            catch (Exception ex)
            {
                logger.LogDebug($"Unable to request Bluetooth LE permissions: {ex.Message}.");
                await Shell.Current.DisplayAlert($"Unable to request Bluetooth LE permissions", $"{ex.Message}.", "OK");
            }
            return status;
        }
            #endregion BluetoothPermissions
#elif IOS
#elif WINDOWS
#endif

        }
    }
