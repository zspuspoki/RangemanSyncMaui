using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RangemanSync.Services.WatchDataReceiver.DataExtractors.Data
{
    internal class LogData
    {
        public DateTime Date { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public long Pressure { get; set; }
        public long Temperature { get; set; }
    }
}
