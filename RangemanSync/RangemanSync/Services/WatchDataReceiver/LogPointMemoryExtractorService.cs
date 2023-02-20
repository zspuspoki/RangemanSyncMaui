using Microsoft.Extensions.Logging;
using Plugin.BLE.Abstractions.Contracts;
using RangemanSync.Services.WatchDataReceiver.DataExtractors;
using RangemanSync.Services.WatchDataReceiver.DataExtractors.Data;
using RangemanSync.Services.WatchDataReceiver.DataExtractors.Header;

namespace RangemanSync.Services.WatchDataReceiver
{
    internal class LogPointMemoryExtractorService
    {
        public event EventHandler<DataReceiverProgressEventArgs> ProgressChanged;
        private RemoteWatchController remoteWatchController;
        private ILogger<LogPointMemoryExtractorService> logger;
        private readonly ILoggerFactory loggerFactory;

        public LogPointMemoryExtractorService(IDevice currentDevice, ILoggerFactory loggerFactory)
        {
            logger = loggerFactory.CreateLogger<LogPointMemoryExtractorService>();
            this.loggerFactory = loggerFactory;
            remoteWatchController = new RemoteWatchController(currentDevice, loggerFactory);
        }

        public async Task<List<LogHeaderDataInfo>> GetHeaderDataAsync()
        {
            try
            {
                await remoteWatchController.SendInitializationCommandsToWatch();

                //REceive StartReadyToTransDataSequence

                var allDataReceived = new TaskCompletionSource<IDataExtractor>();
                var logAndPointMemoryHeaderParser = new LogAndPointMemoryHeaderParser(loggerFactory);

                var casioConvoyAndCasioDataRequestObserver =
                    new CasioConvoyAndCasioDataRequestObserver(logAndPointMemoryHeaderParser,
                        remoteWatchController, allDataReceived, loggerFactory, 0x0F, 8192);

                casioConvoyAndCasioDataRequestObserver.ProgressChanged += CasioConvoyAndCasioDataRequestObserver_ProgressChanged;

                await remoteWatchController.SubscribeToCharacteristicChanges(casioConvoyAndCasioDataRequestObserver);
                IDataExtractor headerResultFromWatch = null;

                var lastTransmissionHasCrcError = false;
                do
                {
                    logger.LogDebug($"Inside lastTransmissionHasCrcError loop");

                    casioConvoyAndCasioDataRequestObserver.RestartDataReceiving(allDataReceived);

                    FireProgressChangedEvent("Sending download commands to watch.");
                    await remoteWatchController.SendDownloadLogCommandsToWatch();

                    FireProgressChangedEvent("Sending download log header commands to watch.");
                    await remoteWatchController.SendDownloadHeaderCommandToWatch();

                    //PreviousDataTransmitReplayer previousDataTransmitReplayer = new PreviousDataTransmitReplayer(casioConvoyAndCasioDataRequestObserver);
                    //previousDataTransmitReplayer.Execute();

                    headerResultFromWatch = await allDataReceived.Task;
                    allDataReceived = new TaskCompletionSource<IDataExtractor>();

                    lastTransmissionHasCrcError = casioConvoyAndCasioDataRequestObserver.HasCrcError;
                } while (lastTransmissionHasCrcError);

                casioConvoyAndCasioDataRequestObserver.ProgressChanged -= CasioConvoyAndCasioDataRequestObserver_ProgressChanged;

                if (headerResultFromWatch != null && headerResultFromWatch is LogAndPointMemoryHeaderParser dataExtractor)
                {
                    var result = new List<LogHeaderDataInfo>();

                    for (var i = 1; i <= 20; i++)
                    {
                        var headerToAdd = dataExtractor.GetLogHeaderDataInfo(i);

                        if (headerToAdd != null)
                        {
                            headerToAdd.OrdinalNumber = i;
                            headerToAdd.LogAddress = dataExtractor.GetLogAddress(i);
                            headerToAdd.LogTotalLength = dataExtractor.GetLogTotalLength(i);
                            result.Add(headerToAdd);
                        }
                    }

                    if (result.Count > 0)
                    {
                        return result;
                    }

                    return null;
                }

                return null;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "An unexpected error occured during getting header data from the watch");
                return null;
            }
        }

        public async Task<List<LogData>> GetLogDataAsync(int headerDataSize, int headerDataCount, int logAddress, int logTotalLength)
        {
            try
            {
                logger.LogDebug("GetLogDataAsync -- Before SendInitializationCommandsToWatch");

                await remoteWatchController.SendInitializationCommandsToWatch();

                logger.LogDebug("GetLogDataAsync -- After SendInitializationCommandsToWatch");

                //REceive StartReadyToTransDataSequence

                var allDataReceived = new TaskCompletionSource<IDataExtractor>();

                logger.LogDebug("GetLogDataAsync -- Before creating observer");
                var casioConvoyAndCasioDataRequestObserver =
                    new CasioConvoyAndCasioDataRequestObserver(
                        new LogDataExtractor(headerDataSize, headerDataCount, loggerFactory),
                            remoteWatchController, allDataReceived, loggerFactory, 16, logTotalLength);

                casioConvoyAndCasioDataRequestObserver.ProgressChanged += CasioConvoyAndCasioDataRequestObserver_ProgressChanged;
                logger.LogDebug("GetLogDataAsync -- After creating observer");

                await remoteWatchController.SubscribeToCharacteristicChanges(casioConvoyAndCasioDataRequestObserver);

                IDataExtractor logDataResultFromWatch = null;
                var lastTransmissionHasCrcError = false;
                do
                {
                    logger.LogDebug("GetLogDataAsync -- Before SendDownloadLogCommandsToWatch");

                    casioConvoyAndCasioDataRequestObserver.RestartDataReceiving(allDataReceived);

                    FireProgressChangedEvent("Sending download commands to watch.");
                    await remoteWatchController.SendDownloadLogCommandsToWatch();
                    logger.LogDebug("GetLogDataAsync -- After SendDownloadLogCommandsToWatch");

                    logger.LogDebug("GetLogDataAsync -- Before SendPointMemoryOrLogDownload");
                    FireProgressChangedEvent("Sending point memory or log download command.");
                    await remoteWatchController.SendPointMemoryOrLogDownload(logAddress, logTotalLength);
                    logger.LogDebug("GetLogDataAsync -- After SendPointMemoryOrLogDownload");

                    logDataResultFromWatch = await allDataReceived.Task;
                    allDataReceived = new TaskCompletionSource<IDataExtractor>();

                    lastTransmissionHasCrcError = casioConvoyAndCasioDataRequestObserver.HasCrcError;
                } while (lastTransmissionHasCrcError);

                casioConvoyAndCasioDataRequestObserver.ProgressChanged -= CasioConvoyAndCasioDataRequestObserver_ProgressChanged;

                if (logDataResultFromWatch != null && logDataResultFromWatch is LogDataExtractor dataExtractor)
                {
                    return dataExtractor.GetAllLogData();
                }

                return null;
            }
            catch(Exception ex)
            {
                logger.LogError(ex, "An unexpected error occured during getting log data from the watch");
                return null;
            }
        }

        private void FireProgressChangedEvent(string message)
        {
            if (ProgressChanged != null)
            {
                ProgressChanged(this, new DataReceiverProgressEventArgs { Text = message });
            }
        }

        private void CasioConvoyAndCasioDataRequestObserver_ProgressChanged(object sender, DataRequestObserverProgressChangedEventArgs e)
        {
            FireProgressChangedEvent(e.Text);
        }

    }

}
