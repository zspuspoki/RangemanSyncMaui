﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RangemanSync.Services.WatchDataReceiver.DataExtractors.Header
{
    internal class LogAndPointMemoryHeaderParser : IDataExtractor
    {
        private const int ONE_LOG_DATA_SIZE = 118784;
        private const int FIRST_LOG_DATA_ADDRESS = 53248;
        private const int FIRST_POINT_MEMORY_DATA_ADDRESS = 2428928;

        private byte[] headerData;
        private ILogger<LogAndPointMemoryHeaderParser> logger;

        public LogAndPointMemoryHeaderParser(ILoggerFactory loggerFactory)
        {
            this.logger = loggerFactory.CreateLogger<LogAndPointMemoryHeaderParser>();
        }

        public void SetData(byte[] headerData)
        {
            this.headerData = headerData;
        }

        public int GetLogAddress(int i)
        {
            return ((i - 1) * ONE_LOG_DATA_SIZE) + FIRST_LOG_DATA_ADDRESS;
        }

        public int getPointMemoryAddress()
        {
            return FIRST_POINT_MEMORY_DATA_ADDRESS;
        }

        public LogHeaderDataInfo GetLogHeaderDataInfo(int i)
        {
            try
            {
                logger.LogDebug("Inside GetLogHeaderDataInfo v9.");

                if (headerData.Length == 0)
                {
                    return null;
                }

                int i2 = (i * 64) + 0;
                var currentDataArray = headerData;

                logger.LogDebug($"Endianness: IsLittleEndian: {BitConverter.IsLittleEndian}");

                int i3 = i2 + 12;
                logger.LogDebug("1");
                int i4 = (currentDataArray[i2] & 255) | ((currentDataArray[i2 + 1] & 255) << 8);
                logger.LogDebug("2");
                int i5 = (currentDataArray[i2 + 4] & 255) | ((currentDataArray[i2 + 5] & 255) << 8);
                logger.LogDebug("3");
                int i6 = (currentDataArray[i2 + 6] & 255) | ((currentDataArray[i2 + 7] & 255) << 8);
                logger.LogDebug("4");
                int i7 = currentDataArray[i2 + 11] & 255;
                logger.LogDebug("5");
                int i8 = currentDataArray[i3] & 255;

                logger.LogDebug($"i3={i3} , i4={i4}, i5={i5}, i6={i6}, i7={i7}, i8={i8}  ");

                var year = i6;
                var month = (currentDataArray[i2 + 8] & 255);
                var day = currentDataArray[i2 + 9] & 255;
                var hour = currentDataArray[i2 + 10] & 255;
                var minute = (sbyte)i7;
                var second = (sbyte)i8;

                logger.LogDebug($"year = {year}, month= {month}, day={day}, hour={hour}, minute={minute}, second={second}");

                if (year > 0 && month > 0 && day > 0)
                {
                    var date = new DateTime(year, month, day, hour, minute, second);
                    return new LogHeaderDataInfo { DataCount = i4, DataSize = i5, Date = date };
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occured during getting the log header data info");
                return null;
            }
        }

        public int GetLogTotalLength(int i)
        {
            int i2 = (i * 64) + 0;
            int i3 = i2 + 5;

            if (headerData.Length < i3)
            {
                return 0;
            }
            return ((headerData[i2] & 255) | ((headerData[i2 + 1] & 255) << 8)) * ((headerData[i2 + 4] & 255) | ((headerData[i3] & 255) << 8));
        }

        public int GetPointMemoryDataCount()
        {
            if (4160 < headerData.Length)
            {
                return headerData[4160] & 255;
            }
            return 0;
        }

        public int getPointMemoryTotalLength()
        {
            return GetPointMemoryDataCount() * 35;
        }

    }

}
