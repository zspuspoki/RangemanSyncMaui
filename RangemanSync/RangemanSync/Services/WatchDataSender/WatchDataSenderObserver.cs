using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions.Contracts;
using RangemanSync.Services.Common;

namespace RangemanSync.Services.WatchDataSender
{
    public class WatchDataSenderObserver : IObserver<Tuple<Guid, byte[]>>
    {
        public enum WorkflowStep
        {
            SendInitCommandsAndWaitForCCCData,
            SendConvoyConnectionParameters,

            SendDataCategoryAndWaitForConnectionParams,
            SendDataConnectionSettingsBasedOnParams,
            DataBufferedConvoySender,

            SendHeaderCategoryAndWaitForConnectionParams,
            SendHeaderConnectionSettingsBasedOnParams,
            HeaderBufferedConvoySender,

            WriteFinalClosingData
        }

        public event EventHandler<DataSenderProgressEventArgs> ProgressChanged;

        private const int CommandDelay = 20;
        private WorkflowStep workflowStep = WorkflowStep.SendInitCommandsAndWaitForCCCData;
        private ILogger<WatchDataSenderObserver> logger;
        private int progressPercent = 8;
        private readonly IDevice currentDevice;
        private readonly ILoggerFactory loggerFactory;
        private readonly IWatchControllerUtilities watchControllerUtilities;
        private readonly byte[] data;
        private readonly byte[] header;

        public WatchDataSenderObserver(IDevice device, ILoggerFactory loggerFactory, 
            IWatchControllerUtilities watchControllerUtilities, 
            byte[] data, byte[] header)
        {
            this.logger = loggerFactory.CreateLogger<WatchDataSenderObserver>();
            this.currentDevice = device;
            this.loggerFactory = loggerFactory;
            this.watchControllerUtilities = watchControllerUtilities;
            this.data = data;
            this.header = header;
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public async void OnNext(Tuple<Guid, byte[]> value)
        {
            if (value.Item1.ToString() == BLEConstants.CasioConvoyCharacteristic && workflowStep == WorkflowStep.SendInitCommandsAndWaitForCCCData)
            {
                logger.LogDebug($"--- SendInitCommandsAndWaitForCCCData - NotifyCharacteristicValue. Received data bytes : {Utils.GetPrintableBytesArray(value.Item2)}");

                if (value.Item2.SequenceEqual(new byte[] { 00, 00, 00 }))
                {
                    logger.LogDebug($"--- SendInitCommandsAndWaitForCCCData - NotifyCharacteristicValue. data equals to convoy data : {Utils.GetPrintableBytesArray(new byte[] { 00, 00, 00 })}");
                    workflowStep = WorkflowStep.SendConvoyConnectionParameters;

                    FireProgressEvent(ref progressPercent, 8, "Sent init commands and waited for CCC data");

                    await SendConvoyConnectionParameters();

                    FireProgressEvent(ref progressPercent, 8, "Sent convoy connection parameters");

                    workflowStep = WorkflowStep.SendDataCategoryAndWaitForConnectionParams;

                    await SendCategoryAndWaitForConnectionParams(0x16);
                }

                return;
            }

            if (value.Item1.ToString() == BLEConstants.CasioConvoyCharacteristic && workflowStep == WorkflowStep.SendDataCategoryAndWaitForConnectionParams)
            {
                FireProgressEvent(ref progressPercent, 8, "Sent category and waited for connection params");

                logger.LogDebug($"--- SendCategoryAndWaitForConnectionParams - NotifyCharacteristicValue. Received data bytes : {Utils.GetPrintableBytesArray(value.Item2)}");
                var kindData = value.Item2[0];

                if (kindData == 2 || kindData == 6)
                {
                    var connectionParameters = new ConnectionParameters(value.Item2);
                    workflowStep = WorkflowStep.SendDataConnectionSettingsBasedOnParams;
                    await SendConnectionSettingsBasedOnParams(connectionParameters, data.Length, 0x16);
                }

                return;
            }

            if (value.Item1.ToString() == BLEConstants.CasioDataRequestSPCharacteristic && workflowStep == WorkflowStep.SendDataConnectionSettingsBasedOnParams)
            {
                FireProgressEvent(ref progressPercent, 8, "Sent connection settings based on params");

                logger.LogDebug("-- DataBufferedConvoySender step: About to start BufferedConvoySender");
                workflowStep = WorkflowStep.DataBufferedConvoySender;
                BufferedConvoySender bufferedConvoySender = new BufferedConvoySender(currentDevice, watchControllerUtilities, data, loggerFactory);
                await bufferedConvoySender.Send();

                FireProgressEvent(ref progressPercent, 8, $"Finished using buffered convoy sender. Category = 0x16");

                logger.LogDebug("Finished using BufferedConvoySender in DataBufferedConvoySender");
                return;
            }

            if (value.Item1.ToString() == BLEConstants.CasioDataRequestSPCharacteristic && workflowStep == WorkflowStep.DataBufferedConvoySender)
            {
                if (value.Item2.SequenceEqual(new byte[] { 09, 0x16, 00, 00, 00, 00, 00 }))
                {
                    logger.LogDebug("--- CloseCurrentCategoryAndWaitForResponse - Sequence is equals");
                    await CloseCurrentCategoryAndWaitForResponse(0x16);

                    FireProgressEvent(ref progressPercent, 8, "Closed current category and waited for response");

                    workflowStep = WorkflowStep.SendHeaderCategoryAndWaitForConnectionParams;
                    await SendCategoryAndWaitForConnectionParams(0x15);
                }

                return;
            }

            if (value.Item1.ToString() == BLEConstants.CasioConvoyCharacteristic && workflowStep == WorkflowStep.SendHeaderCategoryAndWaitForConnectionParams)
            {
                FireProgressEvent(ref progressPercent, 8, "Sent category and waited for connection params");

                logger.LogDebug($"--- SendCategoryAndWaitForConnectionParams - NotifyCharacteristicValue. Received data bytes : {Utils.GetPrintableBytesArray(value.Item2)}");
                var kindData = value.Item2[0];

                if (kindData == 2 || kindData == 6)
                {
                    var connectionParameters = new ConnectionParameters(value.Item2);
                    workflowStep = WorkflowStep.SendHeaderConnectionSettingsBasedOnParams;
                    await SendConnectionSettingsBasedOnParams(connectionParameters, header.Length, 0x15);
                }

                return;
            }

            if (value.Item1.ToString() == BLEConstants.CasioDataRequestSPCharacteristic && workflowStep == WorkflowStep.SendHeaderConnectionSettingsBasedOnParams)
            {
                FireProgressEvent(ref progressPercent, 8, "Sent connection settings based on params");

                workflowStep = WorkflowStep.HeaderBufferedConvoySender;
                BufferedConvoySender bufferedConvoySender = new BufferedConvoySender(currentDevice, watchControllerUtilities, header, loggerFactory);
                await bufferedConvoySender.Send();

                FireProgressEvent(ref progressPercent, 8, $"Finished using buffered convoy sender. Category = 0x15");

                return;
            }

            if (value.Item1.ToString() == BLEConstants.CasioDataRequestSPCharacteristic && workflowStep == WorkflowStep.HeaderBufferedConvoySender)
            {
                if (value.Item2.SequenceEqual(new byte[] { 09, 0x15, 00, 00, 00, 00, 00 }))
                {
                    logger.LogDebug("--- CloseCurrentCategoryAndWaitForResponse - Sequence is equals");
                    await CloseCurrentCategoryAndWaitForResponse(0x15);

                    workflowStep = WorkflowStep.WriteFinalClosingData;
                    await WriteFinalClosingData();
                }

                return;
            }

            if (value.Item1.ToString() == BLEConstants.CasioConvoyCharacteristic && workflowStep == WorkflowStep.WriteFinalClosingData)
            {
                FireProgressEvent(ref progressPercent, 8, "Finished writing final closing data");
                await WriteFinalClosingData2();

                progressPercent = 100;
                FireProgressEvent(ref progressPercent, 0, "Finished sending data");
            }
        }

        private async Task SendConvoyConnectionParameters()
        {
            logger.LogDebug("--- SendConvoyConnectionParameters - Before writing CasioConvoyCharacteristic");
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioConvoyCharacteristic), new byte[] { 0x04, 0x01, 0x18, 0x00, 0x18, 0x00, 0x00, 0x00, 0x58, 0x02 });

        }

        private async Task SendCategoryAndWaitForConnectionParams(byte categoryId)
        {

            logger.LogDebug("--- SendCategoryAndWaitForConnectionParams - Before writing CasioDataRequestSPCharacteristic");
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                                  Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), new byte[] { 0x00, categoryId, 0x00, 0x00 });

            await Task.Delay(CommandDelay);
        }

        private async Task SendConnectionSettingsBasedOnParams(ConnectionParameters parameters, int totalDataLength, byte categoryId)
        {
            long currentParameterDataSizeOf1Sector = parameters.DataSizeOf1Sector * parameters.OffsetSector;
            long j3 = totalDataLength - currentParameterDataSizeOf1Sector;

            logger.LogDebug($"--- SendConnectionSettingsBasedOnParams - currentParameterDataSizeOf1Sector : {currentParameterDataSizeOf1Sector}");
            logger.LogDebug($"--- SendConnectionSettingsBasedOnParams - j3 : {j3}");

            if (j3 > 0)
            {
                //Create convoy data: totalDataLength, j3, acceptor1, acceptor2, timeoutMinute
                //Acceptor1: 250
                //Acceptor2: 245

                var convoyData = CreateConvoyData(totalDataLength, j3, 250, 245, 0);

                logger.LogDebug($"--- SendConnectionSettingsBasedOnParams - after CreateConvoyData. convoyData = {Utils.GetPrintableBytesArray(convoyData)}");

                await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                        Guid.Parse(BLEConstants.CasioConvoyCharacteristic), convoyData);

            }

        }

        private async Task CloseCurrentCategoryAndWaitForResponse(byte categoryId)
        {
            var taskCompletionSource = new TaskCompletionSource<byte[]>();
            logger.LogDebug($"--- CloseCurrentCategoryAndWaitForResponse - Before sending values to characteristic");
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), new byte[] { 0x09, categoryId, 0x00, 0x00, 0x00 });

        }

        public async Task WriteFinalClosingData()
        {
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic), new byte[] { 0x04, 0x12, 0x00, 0x00, 0x00 });  // category 18
        }

        public async Task WriteFinalClosingData2()
        {
            logger.LogDebug($"--- WriteFinalClosingData2 - Start");
            await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                Guid.Parse(BLEConstants.CasioConvoyCharacteristic), new byte[] { 0x04, 0x01, 0x48, 0x00, 0x50, 0x00, 0x04, 0x00, 0x58, 0x02 });

            logger.LogDebug("--- WriteFinalClosingData2 - End");
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

        private static byte[] CreateConvoyData(long j, long j2, int i, int i2, int i3)
        {
            return new byte[] { 1, (byte)(j & 255), (byte)((j >>> 8) & 255), (byte)((j >>> 16) & 255), (byte)((j >>> 24) & 255), (byte)(j2 & 255), (byte)((j2 >>> 8) & 255), (byte)((j2 >>> 16) & 255), (byte)((j2 >>> 24) & 255), (byte)(i & 255), (byte)(i2 & 255), (byte)(i3 & 255) };
        }
    }
}
