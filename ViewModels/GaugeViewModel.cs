namespace BatMan2.ViewModels
{
    public class GaugeViewModel : BaseViewModel
    {
        private double _V;
        private double _A;
        private double _W;
        private double _Wi;
        private double _Wo;
        private double _WH;
        private double _WH_max;
        private double _WH_Alert;
        private double _DailyCon;
        private string _lastBattery = "";

        private DateTime _DT;

        public Command HistoryCommand { get; }
        public Command UpdateCommand { get; }
        public Command SetupCommand { get; }
        public Command AnalysisCommand { get; }
        public Command TapCommand { get; }

        public double V
        {
            get => _V;
            set
            {
                SetProperty(ref _V, value);
            }
        }

        public double A
        {
            get => _A;
            set
            {
                SetProperty(ref _A, value);
            }
        }

        public double W
        {
            get => _W;
            set
            {
                SetProperty(ref _W, value);
            }
        }
        public double Wi
        {
            get => _Wi;
            set
            {
                SetProperty(ref _Wi, value);
            }
        }
        public double Wo
        {
            get => _Wo;
            set
            {
                SetProperty(ref _Wo, value);
            }
        }

        public double WH
        {
            get => _WH;
            set
            {
                SetProperty(ref _WH, value);
            }
        }

        public double WH_max
        {
            get => _WH_max;
            set
            {
                SetProperty(ref _WH_max, value);
            }
        }

        public double WH_Alert
        {
            get => _WH_Alert;
            set
            {
                SetProperty(ref _WH_Alert, value);
            }
        }

        public DateTime DT
        {
            get => _DT;
            set
            {
                SetProperty(ref _DT, value);
            }
        }

        public double DailyCon
        {
            get => _DailyCon;
            set
            {
                SetProperty(ref _DailyCon, value);
            }
        }

        private bool isConnecting = false;
        public bool IsConnecting
        {
            get => isConnecting;
            set
            {
                SetProperty(ref isConnecting, value);
            }
        }

        public GaugeViewModel(IReadingStore<Reading> readingstore)
        {
            ReadingStore = readingstore;
            if (!Battery.ismock)
            {
                Battery.DataChanged += (o, arg) => GetReading(arg);
            }
            HistoryCommand = new Command(OnViewHistory);
            UpdateCommand = new Command(OnBatteryUpdate);
            SetupCommand = new Command(OnGuageSetup);
            AnalysisCommand = new Command(OnAnalysis);
            TapCommand = new Command(OnGaugeWHRemain_Tapped);

            Battery.Connection += (o, arg) =>
            {
                IsConnecting =  !arg;
            };
        }

        public void ActivateDemo()
        {
            Battery.DataChanged += (o, arg) => GetReading(arg);
        }

        public void OnAppearing()
        {
            Title = $"{Battery.ConnectedDevice.Name} - Panel";
            WH_max = Preferences.Get(Battery.ConnectedDevice?.Name + "_wh", 400);
            WH_Alert = Preferences.Get(Battery.ConnectedDevice?.Name + "_alert", 100);
            //if (Device.RuntimePlatform == Device.iOS) {
            //    CrossDeviceOrientation.Current.LockOrientation(DeviceOrientations.Portrait);
            //}
            DT = DateTime.Now;
            if (_lastBattery != Battery.ConnectedDevice.Name)
            {
                RefreshLastWH();
                _lastBattery = Battery.ConnectedDevice.Name;
            }
        }

        private void OnGaugeWHRemain_Tapped(object o)
        {
            if (DateTime.Now.Subtract(DT) > new TimeSpan(0, 0, 10)) {
                Battery.CheckConnection();
            }
            // do work
        }

        private async void GetReading(DataEventArgs arg)
        {
            Console.WriteLine($"Battery class call VM {arg.DT.ToLongTimeString()}");
            V = arg.V;
            A = arg.A;
            W = arg.W;
            WH = arg.WH;
            DT = arg.DT;
            Wo = arg.W;
            if (WH == 0)
            {
                return;
            }
            //Console.WriteLine($"History? {Battery.ConnectedDevice.Name} {ReadingStore.LastDeviceName} {arg.DT.ToLongTimeString()} {ReadingStore.LastHistoryDate.ToLongTimeString()}");
            if (Battery.ConnectedDevice.Name != ReadingStore.LastDeviceName ||
                arg.DT.Subtract(ReadingStore.LastHistoryDate) > new TimeSpan(0, 5, 0))
            {
                Reading reading = new Reading()
                {
                    V = arg.V,
                    A = arg.A,
                    W = arg.W,
                    WH = arg.WH,
                    DT = arg.DT,
                    Battery = Battery.ConnectedDevice.Name
                };
                int rows = await ReadingStore.AddReadingAsync(reading);
                RefreshLastWH();
            } else
            {
                if (W < 0)
                {
                    DailyCon += -1 * W * 5f / 60f / 60f;
                }

            }
        }

        private async void RefreshLastWH()
        {
            var con = (await ReadingStore.GetConsumptionsAsync(Battery.ConnectedDevice.Name, true))
                .LastOrDefault();
            if (con?.DT.Date == DateTime.Now.Date)
            {
                Console.WriteLine($"Refresh daily con {DailyCon} -> {con.WH_out}");
                DailyCon = con.WH_out;
            } else
            {
                DailyCon = 0;
            }
        }

        private async void OnViewHistory(object obj)
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }
        private async void OnBatteryUpdate(object obj)
        {
            await Shell.Current.GoToAsync($"{ nameof(UpdatePage)}?WH={Math.Truncate(WH)}");
        }
        private async void OnGuageSetup(object obj)
        {
            await Shell.Current.GoToAsync(nameof(SetupPage));
        }
        private async void OnAnalysis(object obj)
        {
            await Shell.Current.GoToAsync($"{ nameof(AnalysisPage)}?BatteryName={Battery.ConnectedDevice.Name}");
        }
    }
}
