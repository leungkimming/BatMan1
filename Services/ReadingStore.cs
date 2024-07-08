using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SQLite;
using System.IO;
using Plugin.BLE.Abstractions.Contracts;
using Sylvan.Data.Csv;

namespace BatMan2.Services
{
    public class ReadingStore : IReadingStore<Reading>
    {
        SQLiteAsyncConnection db;
        //IBattery Battery => DependencyService.Get<IBattery>();

        public string LastDeviceName { get; set; }
        public DateTime LastHistoryDate { get; set; }

        public ReadingStore()
        {
            SQLiteOpenFlags Flags =
                SQLiteOpenFlags.ReadWrite |
                SQLiteOpenFlags.Create |
                SQLiteOpenFlags.SharedCache;
            string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BatMan2.db3");
            db = new SQLiteAsyncConnection(dbPath, Flags);
            Task.Run(() => this.db.CreateTableAsync<Reading>()).Wait();
            Reading lastReading = db.Table<Reading>().OrderByDescending(S => S.DT).FirstOrDefaultAsync().Result;
            if (lastReading != null)
            {
                LastDeviceName = lastReading.Battery;
                LastHistoryDate = lastReading.DT;
            } else
            {
                LastDeviceName = "";
                LastHistoryDate = DateTime.MinValue;
            }
            Task.Run(() => this.Compress(DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0)).Date));
        }

        public async Task<bool> LoadDB()
        {
            var result = await FilePicker.PickAsync();
            if (result == null)
            {
                return false;
            }
            CsvDataReaderOptions opt = new CsvDataReaderOptions();
            opt.DateFormat = "dd/MM/yyyy HH:mm:ss";
            try
            {
                using (var csv = await CsvDataReader.CreateAsync(result.FullPath, opt))
                {
                    while (await csv.ReadAsync())
                    {
                        var Battery = csv.GetString(0);
                        var date = csv.GetDateTime(1);
                        var WH = csv.GetDouble(2);
                        var W = csv.GetDouble(3);
                        var A = csv.GetDouble(4);
                        var V = csv.GetDouble(5);
                        await this.AddReadingAsync(new Reading()
                        {
                            DT = date,
                            Battery = Battery,
                            WH = WH,
                            W = W,
                            A = A,
                            V = V
                        });
                    }
                }
            } catch (Exception e)
            {
                throw new Exception(e.Message);
            }
            return true;
        }

        public async Task<int> AddReadingAsync(Reading item)
        {
            int rows = 0;
            try
            {
                rows = await db.InsertAsync(item);
                LastDeviceName = item.Battery;
                LastHistoryDate = item.DT;
            } catch
            {
                rows = -1;
            }
            Console.WriteLine($"VM add record to history {item.DT.ToLongTimeString()} result {rows}");
            return rows;
        }

        public async Task<bool> ClearDBAsync(string? Device)
        {
            bool res = false;
            try
            {
                if (Device == null)
                {
                    LastDeviceName = "";
                    LastHistoryDate = DateTime.MinValue;
                    await db.DropTableAsync<Reading>();
                    await db.CreateTableAsync<Reading>();
                } else
                {
                    var list = await db.Table<Reading>().Where(S => S.Battery == Device).ToListAsync();
                    foreach (Reading r in list)
                    {
                        await db.DeleteAsync<Reading>(r.Id);
                    }
                }
                res = true;
            }
            catch { };
            return res;
        }

        public async Task<IEnumerable<Reading>> GetReadingsAsync(string batt = "")
        {
            List<Reading> res;
            if (batt == "")
            {
                res = await db.Table<Reading>().OrderByDescending(S => S.DT).ToListAsync();
            } else
            {
                res = await db.Table<Reading>()
                    .Where(x => x.Battery == batt).OrderByDescending(S => S.DT).ToListAsync();
            }
            Console.WriteLine($"Read history {batt} row count: {res.Count()}");
            return res;
        }

        public async Task<IEnumerable<Consumption>> GetConsumptionsAsync(string batt, bool today = false)
        {
            DateTime Start;
            if (today)
            {
                Start = DateTime.Now.Date.Subtract(new TimeSpan(1, 0, 0, 0));
            } else
            {
                Start = DateTime.MinValue;
            }
            var items = await db.Table<Reading>()
                .Where(x => x.Battery == batt && x.DT > Start)
                .OrderBy(y => y.DT).ToArrayAsync();
            if (items.Count() == 0 || items.FirstOrDefault().DT.Date == DateTime.Now.Date)
            {
                Start = DateTime.MinValue;
                items = await db.Table<Reading>()
                    .Where(x => x.Battery == batt && x.DT > Start)
                    .OrderBy(y => y.DT).ToArrayAsync();
            }
            return Readings2Consumption(items);
        }

        public async Task<string?[]> GetBatteriesAsync()
        {
            var items = await this.GetReadingsAsync();
            return items.Select(x => x.Battery).Distinct().OrderBy(y => y).ToArray();
        }

        private IEnumerable<Consumption> Readings2Consumption(Reading[] readings)
        {
            DateTime lastDate = DateTime.MinValue;
            double lastConsumption = 0;
            double cConsumption = 0;
            double cCharging = 0;
            Consumption con;
            List<Consumption> cons = new List<Consumption>();

            foreach (Reading csv in readings)
            {
                var date = csv.DT;
                var WH = csv.WH;
                var W = csv.W;
                var A = csv.A;
                var V = csv.V;

                double diff = 0;
                DateTime lastDateEnd, cDateStart;
                double lastSeconds, cSeconds;

                if (lastDate == DateTime.MinValue)
                {
                    lastDate = date;
                    lastConsumption = WH;
                }

                if ((W + A + V) == 0)
                {
                    lastConsumption = WH;
                }

                if (date.Date == lastDate.Date)
                {
                    diff = WH - lastConsumption;
                    if (diff < 0)
                    {
                        cConsumption -= diff;
                    }
                    else
                    {
                        cCharging += diff;
                    }
                    lastConsumption = WH;
                    lastDate = date; //update the time portion for calculation
                }
                else
                {
                    diff = WH - lastConsumption;
                    lastDateEnd = new DateTime(lastDate.Year, lastDate.Month, lastDate.Day, 23, 59, 59);
                    cDateStart = date.Date;
                    lastSeconds = (lastDateEnd - lastDate).TotalSeconds;
                    cSeconds = (date - cDateStart).TotalSeconds;
                    if (diff < 0)
                    {
                        cConsumption -= diff * lastSeconds / (lastSeconds + cSeconds);
                    }
                    else
                    {
                        cCharging += diff * lastSeconds / (lastSeconds + cSeconds);
                    }
                    if (cConsumption > 10) // only histories > 10WH
                    {
                        con = new Consumption()
                        {
                            DT = lastDate.Date,
                            WH_in = cCharging,
                            WH_out = cConsumption,
                        };
                        cons.Add(con);
                    }
                    if (diff < 0)
                    {
                        cConsumption = -1 * diff * cSeconds / (lastSeconds + cSeconds);
                        cCharging = 0;
                    }
                    else
                    {
                        cCharging = diff * cSeconds / (lastSeconds + cSeconds);
                        cConsumption = 0;
                    }
                    lastDate = date;
                    lastConsumption = WH;
                }
            }
            if (lastDate > DateTime.MinValue &&
                (cConsumption > 10 || lastDate.Date == DateTime.Now.Date)) //except today, include < 10WH
            {
                con = new Consumption()
                {
                    DT = lastDate.Date,
                    WH_in = cCharging,
                    WH_out = cConsumption,
                };
                cons.Add(con);
            }

            return cons;
        }

        public async Task<bool> Compress(DateTime before)
        {
            List<Reading> readings;
            string lastbat = "";
            double lastWH = -1;
            string lastDIR = "";
            string currDIR = "";
            DateTime lastdate = DateTime.MinValue;
            int row = 0;
            Reading TBD = null;

            readings = await db.Table<Reading>().Where(d => d.DT < before)
                .OrderBy(S => S.Battery).ThenBy(S => S.DT).ToListAsync();
            foreach (Reading r in readings)
            {
                if (r.Battery != lastbat)
                {
                    lastbat = r.Battery;
                    lastdate = r.DT;
                    lastWH = r.WH;
                    TBD = null;
                    continue;
                }

                if (r.DT.Date != lastdate.Date)
                {
                    lastdate = r.DT;
                    lastWH = r.WH;
                    TBD = null;
                    continue;
                }

                if (r.A + r.W + r.V == 0)
                {
                    lastdate = r.DT;
                    lastWH = r.WH;
                    TBD = null;
                    continue;
                }

                currDIR = (r.WH <= lastWH) ? "O":"I";
                if (currDIR != lastDIR)
                {
                    lastDIR = currDIR;
                    TBD = null;
                }
                else
                {
                    if (TBD != null)
                    {
                        try
                        {
                            row += await db.DeleteAsync(TBD);
                        }
                        catch (Exception e)
                        {
                            throw new Exception("Compress error - " + e.Message);
                        }
                    }
                    TBD = r;
                }
                lastWH = r.WH;
            }
            Console.WriteLine($"Records compressed before {before} = {row}");
            return true;
        }
    }
}
