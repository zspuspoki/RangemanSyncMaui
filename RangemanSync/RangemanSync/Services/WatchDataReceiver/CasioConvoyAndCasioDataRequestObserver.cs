﻿using Microsoft.Extensions.Logging;
using RangemanSync.Services.Common;
using RangemanSync.Services.WatchDataReceiver.DataExtractors;

namespace RangemanSync.Services.WatchDataReceiver
{
    internal class CasioConvoyAndCasioDataRequestObserver : IObserver<Tuple<Guid, byte[]>>
    {
        private const int SectorSize = 256;   //The current sector size is set to 256 bytes

        private List<byte[]> data = new List<byte[]>();
        private int currentSectorIndex = 0;
        private int currentDataIndexOnCurrentSector = 0;
        private int headerSize;

        private int digestedByteCount = 0; // every 256 bytes - we have a CRC code
        private bool dataReceivingIsAllowed = true;
        private bool casioConvoyDataReceivingIsAllowed = false;

        private bool successFullyEndedTransmission = false;

        private static readonly object key = new object();
        private IDataExtractor dataExtractor;
        private readonly RemoteWatchController remoteWatchController;
        private TaskCompletionSource<IDataExtractor> taskCompletionSource;
        private readonly byte categoryId;
        private readonly int watchSectorSize;
        private ILogger<CasioConvoyAndCasioDataRequestObserver> logger;
        private List<byte> last256Bytes = new List<byte>();

        private bool hasCRCError = false;

        public bool HasCrcError { get => hasCRCError; }

        public event EventHandler<DataRequestObserverProgressChangedEventArgs> ProgressChanged;

        public CasioConvoyAndCasioDataRequestObserver(IDataExtractor dataExtractor,
            RemoteWatchController remoteWatchController,
            TaskCompletionSource<IDataExtractor> taskCompletionSource,
            ILoggerFactory loggerFactory,
            byte categoryId, int watchSectorSize)
        {
            this.dataExtractor = dataExtractor;
            this.remoteWatchController = remoteWatchController;
            this.taskCompletionSource = taskCompletionSource;
            this.categoryId = categoryId;
            this.watchSectorSize = watchSectorSize;
            this.logger = loggerFactory.CreateLogger<CasioConvoyAndCasioDataRequestObserver>();

            logger.LogDebug($"Inside constructor. CategoryId = {categoryId} watchSectorSize = {watchSectorSize}");
        }

        public void OnCompleted()
        {
            logger.LogDebug("-- Finished with CasioConvoyAndCasioDataRequestObserver");

            if (!successFullyEndedTransmission)
            {
                dataExtractor.SetData(new byte[] { });
            }

            taskCompletionSource.TrySetResult(this.dataExtractor);
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(Tuple<Guid, byte[]> value)
        {
            lock (key)
            {
                if (!dataReceivingIsAllowed)
                {
                    logger.LogDebug("OnNext - CasioConvoyAndCasioDataRequestObserver - data receiving is not allowed. Returning.");
                    return;
                }

                logger.LogDebug($"OnNext - CasioConvoyAndCasioDataRequestObserver  Guid = {value.Item1}  value = {Utils.GetPrintableBytesArray(value.Item2)}");

                if (casioConvoyDataReceivingIsAllowed && value.Item1 == Guid.Parse(BLEConstants.CasioConvoyCharacteristic))
                {
                    if (value.Item2.Length > 0 && value.Item2[0] == 5)
                    {
                        var bytesToAdd = value.Item2.ToList();
                        bytesToAdd.RemoveAt(0);  //Remove type code

                        digestedByteCount += bytesToAdd.Count;

                        foreach (var dataByte in bytesToAdd)
                        {
                            last256Bytes.Add((byte)~dataByte);
                        }

                        if (digestedByteCount >= 256)
                        {
                            digestedByteCount = 0;

                            var crcByte1 = bytesToAdd[bytesToAdd.Count - 1];
                            var crcByte2 = bytesToAdd[bytesToAdd.Count - 2];
                            var receivedCrc = new byte[] { crcByte2, crcByte1 };

                            bytesToAdd.RemoveAt(bytesToAdd.Count - 1); // Remove two bytes CRC code from the end
                            bytesToAdd.RemoveAt(bytesToAdd.Count - 1); // Remove two bytes CRC code from the end

                            last256Bytes.RemoveAt(last256Bytes.Count - 1);
                            last256Bytes.RemoveAt(last256Bytes.Count - 1);

                            Crc16 crc = new Crc16(Crc16Mode.CcittKermit);
                            var checksumBytes = crc.ComputeChecksumBytes(last256Bytes.ToArray());

                            //logger.LogDebug($"Last256byte length = {last256Bytes.Count} Last 256 bytes for CRC calc : {Utils.GetPrintableBytesArray(last256Bytes.ToArray())}");
                            //logger.LogDebug($"Calculated CRC: {Utils.GetPrintableBytesArray(checksumBytes)}, CRC Byte 1: {crcByte1}, CRC Byte 2: {crcByte2}");
                            if (!checksumBytes.SequenceEqual(receivedCrc))
                            {
                                hasCRCError = true;
                                logger.LogDebug($"CRC error found ! HasCRCError was set. Used array to calculate: {Utils.GetPrintableBytesArray(last256Bytes.ToArray())}");
                                logger.LogDebug("--------------- Restarting transmission -----------");
                                remoteWatchController.AskWatchToEndTransmission(categoryId);
                                EndCurrentTransmission();
                                return;
                            }

                            last256Bytes.Clear();
                        }

                        var bytesArrayToAdd = bytesToAdd.ToArray();

                        for (var i = 0; i < bytesToAdd.Count; i++)
                        {
                            bytesArrayToAdd[i] = (byte)(~bytesArrayToAdd[i]);
                        }

                        //logger.LogDebug($"OnNext - CasioConvoyAndCasioDataRequestObserver data length = {data.Count}");
                        byte[] currentSectorBytes = data[currentSectorIndex];

                        //if (currentDataIndexOnCurrentSector + bytesArrayToAdd.Length > currentSectorBytes.Length - 1)
                        if (SectorSize - currentDataIndexOnCurrentSector < bytesArrayToAdd.Length)
                        {
                            currentSectorIndex++;
                            currentSectorBytes = data[currentSectorIndex];
                            currentDataIndexOnCurrentSector = 0;
                        }

                        Array.Copy(bytesArrayToAdd, 0, currentSectorBytes, currentDataIndexOnCurrentSector, bytesArrayToAdd.Length);
                        currentDataIndexOnCurrentSector += bytesArrayToAdd.Length;

                        var sectorOffsetFromStart = currentSectorIndex * SectorSize;

                        FireProgressChanged($"Reading data from the watch. Sector offset from start: {sectorOffsetFromStart}");
                        logger.LogDebug($"OnNext - CasioConvoyAndCasioDataRequestObserver - Current sector offset : {sectorOffsetFromStart} Data index of sector: {currentDataIndexOnCurrentSector}");

                        if (headerSize == sectorOffsetFromStart + currentDataIndexOnCurrentSector)
                        {
                            logger.LogDebug("OnNext - CasioConvoyAndCasioDataRequestObserver - all the header data was received successfully.");

                            EndCurrentTransmission();
                        }
                    }
                }
                else if (value.Item1 == Guid.Parse(BLEConstants.CasioDataRequestSPCharacteristic))
                {
                    var receivedBytes = value.Item2;
                    if (value.Item2.Length >= 9 && receivedBytes[0] == 0x00 && receivedBytes[1] == categoryId)
                    {
                        logger.LogDebug("OnNext - CasioConvoyAndCasioDataRequestObserver - CasioDataRequestSPCharacteristic : Received an array where the length >= 9");
                        headerSize = ((receivedBytes[9] & 255) << 24) | (receivedBytes[6] & 255) | ((receivedBytes[7] & 255) << 8) | ((receivedBytes[8] & 255) << 16);
                        logger.LogDebug($"OnNext - CasioConvoyAndCasioDataRequestObserver - CasioDataRequestSPCharacteristic: Header size: {headerSize}");

                        var numberofSectorToBeAdded = headerSize / SectorSize + 1;

                        logger.LogDebug($"OnNext - CasioConvoyAndCasioDataRequestObserver - CasioDataRequestSPCharacteristic - No of sectors : {numberofSectorToBeAdded}");

                        for (var i = 0; i < numberofSectorToBeAdded; i++)
                        {
                            byte[] emptySector = new byte[SectorSize];
                            data.Add(emptySector);
                        }

                        casioConvoyDataReceivingIsAllowed = true;
                    }
                    else
                    {
                        if (receivedBytes.Length > 0)
                        {
                            if (receivedBytes[0] == 7)
                            {
                                logger.LogDebug("-- The watch needs confirmation to continue sending the data, so let's send it back.");
                                remoteWatchController.SendConfirmationToContinueTransmission();
                            }
                            else if (receivedBytes.Length >= 2 && receivedBytes[0] == 0x09 && receivedBytes[1] == 0x10)
                            {
                                //TODO : end current transmission because we hve all the data that is needed
                                EndCurrentTransmission();
                            }
                            else
                            {
                                if (receivedBytes.SequenceEqual(new byte[] { 0x09, 0x0F, 0x00, 0x00, 0x00, 0x00, 0x00 }))
                                {
                                    EndCurrentTransmission();
                                }
                            }
                        }
                    }
                }
            }
        }

        private void EndCurrentTransmission()
        {
            dataReceivingIsAllowed = false;
            casioConvoyDataReceivingIsAllowed = false;

            var allReceivedData = Utils.GetAllDataArray(data);
            dataExtractor.SetData(allReceivedData);

            if (taskCompletionSource != null)
            {
                taskCompletionSource.SetResult(dataExtractor);
                successFullyEndedTransmission = true;
            }
        }

        public void RestartDataReceiving(TaskCompletionSource<IDataExtractor> taskCompletionSource)
        {
            this.taskCompletionSource = taskCompletionSource;

            RestartDataReceiving();
        }

        public void RestartDataReceiving()
        {
            ReinitData();
            dataReceivingIsAllowed = true;
        }

        public void SetDataExtractor(IDataExtractor dataExtractor)
        {
            this.dataExtractor = dataExtractor;
        }

        private void ReinitData()
        {
            data.Clear();
            currentSectorIndex = 0;
            currentDataIndexOnCurrentSector = 0;
            digestedByteCount = 0;
            last256Bytes.Clear();
            hasCRCError = false;
        }

        private void FireProgressChanged(string message)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, new DataRequestObserverProgressChangedEventArgs { Text = message });
            }
        }
    }

}
