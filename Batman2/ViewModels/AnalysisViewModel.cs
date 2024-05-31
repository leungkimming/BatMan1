using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

using Xamarin.Forms;

using Batman2.Models;
using Batman2.Views;
using System.IO;
using Xamarin.Essentials;
using System.Linq;

using Accord.Statistics;
using Accord.Statistics.Distributions.Univariate;
using Accord.Statistics.Analysis;
using Accord.Statistics.Distributions;
using System.Text.RegularExpressions;
using Microcharts;
using Microcharts.Abstracts;
using SkiaSharp;
using Accord.Math;

namespace Batman2.ViewModels
{
    [QueryProperty(nameof(BatteryName), nameof(BatteryName))]
    public class AnalysisViewModel : BaseViewModel
    {
        private Reading[] _Readings;

        private ObservableCollection<Consumption> _Items;
        public ObservableCollection<Consumption> Items
        {
            get { return _Items; }
            set { SetProperty(ref _Items, value); }
        }
        public Command LoadItemsCommand { get; }
        public Command ShareCommand { get; }

        private string _BatteryName = "";
        public string BatteryName
        {
            get { return _BatteryName; }
            set { SetProperty(ref _BatteryName, value); }
        }

        private string _error = "";
        public string Error
        {
            get { return _error; }
            set { SetProperty(ref _error, value); }
        }

        private string _Model;
        public string Model
        {
            get { return _Model; }
            set { SetProperty(ref _Model, value); }
        }

        private double _Mean;
        public double Mean
        {
            get { return _Mean; }
            set { SetProperty(ref _Mean, value); }
        }

        private double _SD;
        public double SD
        {
            get { return _SD; }
            set { SetProperty(ref _SD, value); }
        }

        private double _Min;
        public double Min
        {
            get { return _Min; }
            set { SetProperty(ref _Min, value); }
        }

        private bool _IsSingle;
        public bool IsSingle
        {
            get { return _IsSingle; }
            set { SetProperty(ref _IsSingle, value); }
        }

        private string _Remaining;
        public string Remaining
        {
            get { return _Remaining; }
            set { SetProperty(ref _Remaining, value); }
        }

        private readonly Regex format = new Regex(@"^(0?[1-9]|[1-9][0-9])$");
        private double _P = 90;
        public string P_s
        {
            get { return _P.ToString(); }
            set {
                if (Error.StartsWith("Percentage")) Error = "";
                if (!format.IsMatch(value))
                {
                    Error = "Percentage must between 1 - 99";
                    InputValid = false;
                    //SetProperty(ref _P, 1);
                }
                else
                {
                    SetProperty(ref _P, double.Parse(value));
                    InputValid = true;
                    //Analyze();
                }
            }
        }

        private bool _Inputvalid;
        public bool InputValid
        {
            get => _Inputvalid;
            set
            {
                SetProperty(ref _Inputvalid, value);
            }
        }

        private Chart _Chart;
        public Chart Chart
        {
            get => _Chart;
            set
            {
                SetProperty(ref _Chart, value);
            }
        }

        public AnalysisViewModel()
        {
            Items = new ObservableCollection<Consumption>();
            LoadItemsCommand = new Command(async () => {
                await ExecuteLoadItemsCommand();
                Analyze();
            });
            ShareCommand = new Command(OnShare);
        }

        public async void OnAppearing()
        {
            IsBusy = false;
            Title = $"{BatteryName} Analysis";
            if (BatteryName == "")
            {
                Title = "All Batteries Analysis";
                IsSingle = false;
            } else
            {
                IsSingle = true;
                var lastWH = (await ReadingStore.GetReadingsAsync(BatteryName)).FirstOrDefault().WH;
                Remaining = $"(Remaining {lastWH:F0})";
            }
            Task.Run(() => this.ExecuteLoadItemsCommand()).Wait();
            if (Error =="") Analyze();
        }

        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;
            _Items = new ObservableCollection<Consumption>();
            Error = "";

            try
            {
                string[] batterys = null;

                if (BatteryName == "")
                {
                    batterys = await ReadingStore.GetBatteriesAsync();
                }
                else
                {
                    batterys = new string[] { BatteryName };
                }

                foreach (string bat in batterys)
                {
                    var con = await ReadingStore.GetConsumptionsAsync(bat);
                    _Items = new ObservableCollection<Consumption>(_Items.Concat(con));
                }
            }
            catch (Exception ex)
            {
                Error = $"SQLite Error: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
            Items = new ObservableCollection<Consumption>(
                _Items.OrderByDescending(y => y.DT));
        }


        public void Analyze()
        {
            double[] observations = new double[] { };
            UnivariateContinuousDistribution modelc = null;

            try
            {
                observations = Items
                    .Where(x => x.WH_out > 10)
                    .Select(x => x.WH_out.Round(5)).ToArray<double>();
                double p = _P / 100d;
                var analysis = new DistributionAnalysis();
                for (int i = 0; i < 5; i++)
                {
                    string gof = analysis.Learn(observations)[i].Name;
                    if (gof == "Normal" || gof == "Gamma")
                    {
                        Model = gof;
                        break;
                    }
                }
                var mostLikely = analysis.Learn(observations)[Model];
                modelc = (UnivariateContinuousDistribution)mostLikely.Distribution;
                Mean = modelc.Mean;
                SD = modelc.StandardDeviation;
                Min = modelc.InverseDistributionFunction(p: p);

                Collection<ChartEntry> chartEntries = new Collection<ChartEntry>();
                var Color = SKColors.DarkGreen;
                double pfd = 0;

                var chartdata = observations.Concat(new double[] { Min.Round(5) }); //add alert point
                foreach (double v in chartdata.Distinct().OrderBy(v => v))
                {
                    if (v > Min.Round(5))
                    {
                        Color = SKColors.Red; // SKColor.Parse("#2c3e50");
                    }
                    pfd = modelc.ProbabilityDensityFunction(v) * 100;
                    chartEntries.Add(new ChartEntry((float)pfd)
                    {
                        Label = v.ToString("F0"),
                        ValueLabel = pfd.ToString("F1"),
                        Color = Color,
                    });
                }

                Chart = new BarChart()
                {
                    Entries = chartEntries,
                    //LineMode = LineMode.Spline,
                    //LineAreaAlpha = 8,
                    //EnableYFadeOutGradient = false,
                    LabelTextSize = 18,
                    Margin = 10,
                    BarAreaAlpha = 1,
                    //ShowYAxisText = true,
                    //YAxisMaxTicks = 5,
                    //YAxisPosition = Position.Left,
                };
            } catch (Exception e)
            {
                Error = "Not enough > 10Whr. Consumptions!";
            }
        }

        private async void OnShare()
        {
            var cacheDir = FileSystem.CacheDirectory;
            var path = Path.Combine(cacheDir, "..", "consumptions.csv");
            using (StreamWriter file = new StreamWriter(path, append: false))
            {
                await file.WriteLineAsync("\"DT\", \"Charged\", \"Consumed\"");
                foreach (var item in Items)
                {
                    await file.WriteLineAsync($"{item.DT.ToString("dd/MM/yyyy")},{item.WH_in.ToString("F4")},{item.WH_out.ToString("F4")}");
                }
                file.Close();
            }
            await Share.RequestAsync(new ShareFileRequest
            {
                File = new ShareFile(path),
                Title = "Consumption Export"
            });
        }
    }
    public static class doubleExtensions
    {
        public static double Round(this double value, int roundTo)
        {
            return (int)(Math.Round(value / roundTo) * roundTo);
        }
    }
}
