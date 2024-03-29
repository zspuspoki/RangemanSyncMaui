﻿using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions.Contracts;
using RangemanSync.Services.Common;

namespace RangemanSync.Services.WatchDataSender
{
    internal class BufferedConvoySender
    {
        private const int MaxNumberOfBytesToWriteConvoy = 107;
        private readonly IDevice currentDevice;
        private readonly IWatchControllerUtilities watchControllerUtilities;
        private readonly byte[] data;
        private ILogger<BufferedConvoySender> logger;

        public BufferedConvoySender(IDevice currentDevice, IWatchControllerUtilities watchControllerUtilities, 
            byte[] data, ILoggerFactory loggerFactory)
        {
            this.currentDevice = currentDevice;
            this.watchControllerUtilities = watchControllerUtilities;
            this.watchControllerUtilities.Device = currentDevice;
            this.data = data;
            this.logger = loggerFactory.CreateLogger<BufferedConvoySender>();
        }

        public async Task Send()
        {
            int i = 0;
            int currentConvoyDataCount = 0;

            List<byte> currentDataToSend = new List<byte>();
            List<byte> oneDataChunkWithCrc = new List<byte>();

            logger.LogDebug($"--- BufferedConvoySender - data.Length = {data.Length}");

            while (i < data.Length)
            {
                currentDataToSend.Add(0x05); // 0x05 is the type code of convoy data

                logger.LogDebug($"--- BufferedConvoySender - currentConvoyDataCount = {currentConvoyDataCount}");

                while (i < data.Length && currentConvoyDataCount++ < MaxNumberOfBytesToWriteConvoy)
                {
                    var dataToAdd = (byte)~(data[i++]);
                    currentDataToSend.Add(dataToAdd);
                    oneDataChunkWithCrc.Add(dataToAdd);

                    if (i % 256 == 0)
                        break;
                }

                logger.LogDebug($"--- BufferedConvoySender - i after while loop = {i}");

                if (i % 256 == 0)
                {
                    var crc16 = new Crc16(Crc16Mode.CcittKermit);
                    var crc = crc16.ComputeChecksumBytes(oneDataChunkWithCrc.ToArray());

                    logger.LogDebug($"--- BufferedConvoySender - crc code  : {Utils.GetPrintableBytesArray(crc)}");

                    foreach (var crcByte in crc)
                    {
                        currentDataToSend.Add(crcByte);
                    }

                    oneDataChunkWithCrc.Clear();
                }

                var currentByteArrayToSend = currentDataToSend.ToArray();

                logger.LogDebug($"-- BufferedConvoySender - before sending data: {Utils.GetPrintableBytesArray(currentByteArrayToSend)}");

                await watchControllerUtilities.WriteCharacteristicValue(Guid.Parse(BLEConstants.CasioFeaturesServiceGuid),
                    Guid.Parse(BLEConstants.CasioConvoyCharacteristic), currentByteArrayToSend);

                currentDataToSend.Clear();
                currentConvoyDataCount = 0;
            }
        }
    }
}
