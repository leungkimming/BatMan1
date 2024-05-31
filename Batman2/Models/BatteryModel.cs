using System;
using SQLite;
using System.Collections.ObjectModel;

namespace Batman2.Models
{
    public enum scanResult
    {
        SingleConected = 1, MultipleReturned
    }

    public class DataEventArgs : EventArgs
    {
        public readonly double V;
        public readonly double A;
        public readonly double W;
        public readonly double WH;
        public readonly DateTime DT;
        public DataEventArgs(double _V, double _A, double _W, double _WH) {
            V = _V;
            A = _A;
            W = _W;
            WH = _WH;
            DT = DateTime.Now;
        }
    }

    public class Reading
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [Indexed]
        public string Battery { get; set; }
        [Indexed]
        public DateTime DT { get; set; }
        public double V { get; set; }
        public double A { get; set; }
        public double W { get; set; }
        public double WH { get; set; }
    }

    public class Consumption
    {
        public DateTime DT { get; set; }
        public double WH_in { get; set; }
        public double WH_out { get; set; }
    }

    public class ReadingGroup: ObservableCollection<Reading>
    {
        public string BatteryG { get; private set; }

        public ReadingGroup (string battery, ObservableCollection<Reading> readings) : base(readings)
        {
            BatteryG = battery;
        }
    }
}
