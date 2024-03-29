﻿namespace RangemanSync.Services.WatchDataReceiver.DataExtractors.Header
{
    internal class LogHeaderDataInfo
    {
        public int DataCount { get; set; }
        public int DataSize { get; set; }
        public DateTime Date { get; set; }
        public int LogAddress { get; set; }
        public int LogTotalLength { get; set; }
        public int OrdinalNumber { get; set; }
    }
}
