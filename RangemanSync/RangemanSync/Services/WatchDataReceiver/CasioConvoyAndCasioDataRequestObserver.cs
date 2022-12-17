using Microsoft.Extensions.Logging;
using RangemanSync.Services.WatchDataReceiver.DataExtractors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RangemanSync.Services.WatchDataReceiver
{
    class CasioConvoyAndCasioDataRequestObserver
    {
        private bool hasCRCError = false;

        public bool HasCrcError { get => hasCRCError; }

        public event EventHandler<DataRequestObserverProgressChangedEventArgs> ProgressChanged;

        public CasioConvoyAndCasioDataRequestObserver(IDataExtractor dataExtractor,
                  RemoteWatchController remoteWatchController,
                  TaskCompletionSource<IDataExtractor> taskCompletionSource,
                  ILoggerFactory loggerFactory,
                  byte categoryId, int watchSectorSize)
        {
        }

        public void RestartDataReceiving(TaskCompletionSource<IDataExtractor> taskCompletionSource)
        {
        }

    }
}
