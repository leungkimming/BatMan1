using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Exceptions;
using System.Text;
using Plugin.BLE.Abstractions.EventArgs;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Net;
using Plugin.BLE.Abstractions;
using System.Threading;
using System.Timers;
using System.IO;
using Sylvan.Data.Csv;

namespace BatMan2.Services
{
    public interface IDeviceExt
    {
        string Custname { get; set; }
    }

    public class MockDevice : IDevice
    {
        public string Custname { get; set; }
        public string Name => Custname;
        public Guid Id => new Guid();
        public int Rssi => 0;

        Guid IDevice.Id => Id;

        string IDevice.Name => Custname; //"ESPBatMon2";

        int IDevice.Rssi => 0;

        object IDevice.NativeDevice => null;

        DeviceState IDevice.State => DeviceState.Connected;

        public IReadOnlyList<AdvertisementRecord> AdvertisementRecords => null;
        public bool IsConnectable => true; // Assuming devices are connectable for mock.

        public bool SupportsIsConnectable => true; // Assuming support for checking connectability.

        public DeviceBondState BondState => DeviceBondState.Bonded; // Assuming devices are bonded for mock.

        void IDisposable.Dispose()
        {
            return;
        }

        Task<IService> IDevice.GetServiceAsync(Guid id, CancellationToken cancellationToken)
        {
            return Task.FromResult<IService>(null);
        }

        Task<IReadOnlyList<IService>> IDevice.GetServicesAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IReadOnlyList<IService>>(null);
        }

        Task<int> IDevice.RequestMtuAsync(int requestValue)
        {
            return Task.FromResult(0);
        }

        bool IDevice.UpdateConnectionInterval(ConnectionInterval interval)
        {
            throw new NotImplementedException();
        }

        Task<bool> IDevice.UpdateRssiAsync()
        {
            return Task.FromResult(true);
        }
        bool IDevice.UpdateConnectionParameters(ConnectParameters parameters) {
            // Mock implementation, assuming parameters are always successfully updated.
            throw new NotImplementedException();
        }
    }

    public class MockBattery : IBatManBattery {
        private bool canScan, canUpdate, isConnected;
        private ObservableCollection<IDevice> deviceList = new ObservableCollection<IDevice>();
        private IDevice connectedDevice;
        //private IBluetoothLE ble;
        //private IAdapter adapter;
        //private IReadOnlyList<IService> Services;
        //private IService Service;

        //private IReadOnlyList<ICharacteristic> Characteristics;
        //private ICharacteristic Characteristic;
        //private ICharacteristic Characteristic_update;

        private System.Timers.Timer _timer;
        private Random RAND = new Random();

        private bool charging = false;
        private double voltage = 28.3;
        private double wh = 123.4;
        private double amp = 2;
        public IReadingStore<Reading> ReadingStore;

        public MockBattery(IReadingStore<Reading> reading)
        {
            ReadingStore = reading;
            canScan = true;
            canUpdate = false;
            isConnected = false;
            //ble = CrossBluetoothLE.Current;
            //adapter = CrossBluetoothLE.Current.Adapter;
            //adapter.ScanMode = ScanMode.LowLatency;
            //adapter.ScanTimeout = 1000; //1 seconds
            Task.Run(() => this.CreateDemo()).Wait();

            _timer = new System.Timers.Timer()
            {
                Interval = 5000
            };
            _timer.Elapsed += OnTimedEvent;
            _timer.Enabled = false;

            try
            {
                Accelerometer.Start(SensorSpeed.UI);
            } catch { }
            Accelerometer.ShakeDetected += Accelerometer_ShakeDetected;
        }

        async Task CreateDemo()
        {
            deviceList = new ObservableCollection<IDevice>();
            var dev1 = new MockDevice();
            dev1.Custname = "DemoBatt1";
            var dev2 = new MockDevice();
            dev2.Custname = "DemoBatt2";
            var dev3 = new MockDevice();
            dev3.Custname = "DemoBatt3";
            deviceList.Add(dev1);
            deviceList.Add(dev2);
            deviceList.Add(dev3);

            var readings = (await ReadingStore.GetReadingsAsync()).Where(x => x.Battery == dev1.Custname);
            if (readings.Count() < 1)
            {
                using (var stream = await FileSystem.OpenAppPackageFileAsync("DemoInit.csv"))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        CsvDataReaderOptions opt = new CsvDataReaderOptions();
                        opt.DateFormat = "dd/MM/yyyy HH:mm:ss";
                        using (var csv = await CsvDataReader.CreateAsync(reader, opt))
                        {
                            while (await csv.ReadAsync())
                            {
                                var Battery = csv.GetString(0);
                                var date = csv.GetDateTime(1);
                                var WH = csv.GetDouble(2);
                                var W = csv.GetDouble(3);
                                var A = csv.GetDouble(4);
                                var V = csv.GetDouble(5);
                                await ReadingStore.AddReadingAsync(new Reading()
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
                    }
                }
            }
        }

        void Accelerometer_ShakeDetected(object sender, EventArgs e)
        {
            if (charging)
            {
                charging = false;
            } else
            {
                charging = true;
                amp = 2;
            }
        }
        private void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            if (charging)
            {
                if (amp > 0.003f)
                {
                    amp -= 0.003f;
                    voltage += 0.0002f;
                    wh += (amp * voltage) / (60f * 60f) * 5f;
                }
                else
                {
                    amp = 0;
                }
            } else
            {
                amp = RAND.Next(-5500, -100) / 1000f;
                voltage -= 0.0002f;
                wh += (amp * voltage) / (60f * 60f) * 5f;
            }
            OnDataChanged(new DataEventArgs(
                voltage,
                amp,
                voltage * amp,
                wh)
            );
        }

        public bool ismock { get => true; }

        public bool CanScan
        {
            get
            {
                return this.canScan;
            }
        }

        public bool CanUpdate
        {
            get
            {
                return this.canUpdate;
            }
        }

        public ObservableCollection<IDevice> DeviceList
        {
            get
            {
                return this.deviceList;
            }
        }

        public IDevice ConnectedDevice
        {
            get
            {
                return this.connectedDevice;
            }
        }

        public bool IsConnected
        {
            get
            {
                return this.isConnected;
            }
            set
            {
                this.isConnected = value;
                Connection?.Invoke(this, value);
            }
        }

        public event EventHandler<DataEventArgs> DataChanged;

        protected virtual void OnDataChanged(DataEventArgs e)
        {
            DataChanged?.Invoke(this, e);
        }

        public event EventHandler<IDevice> DeviceFound;

        public event EventHandler<bool> Connection;

        protected virtual void OnDeviceFound(IDevice e)
        {
            DeviceFound?.Invoke(this, e);
        }

        public void CheckConnection()
        {
        }

        public void Disconnect(bool cleanup) {
            connectedDevice = null;
            canUpdate = false;
            IsConnected = false;
            _timer.Enabled = false;
        }

        public async Task<bool> ConnectAsync(IDevice item)
        {
            Disconnect(false);
            connectedDevice = item;
            canUpdate = true;
            IsConnected = true;
            wh = Preferences.Get(connectedDevice.Name + "_wh", 300);
            _timer.Enabled = true;

            return true;
        }

        public async Task<scanResult> ScanAndConnectAsyc()
        {
            var result = scanResult.MultipleReturned;
            try
            {
                Disconnect(false);
                OnDeviceFound(DeviceList[0]);
                OnDeviceFound(DeviceList[1]);
                OnDeviceFound(DeviceList[2]);
                if (true)
                {
                    canScan = false;
                    if (deviceList.Count == 1)
                    {
                        await ConnectAsync(deviceList[0]);
                        result = scanResult.SingleConected;
                    }
                    canScan = true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Scan/Connect error", ex);
            }
            return result;
        }

        public async Task<bool> UpdateAsync(Int16 value)
        {
            if (!isConnected)
            {
                throw new Exception("Please connect first!");
            }
            wh = value;
            return true;
        }
    }
}
