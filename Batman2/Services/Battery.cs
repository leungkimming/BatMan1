using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Batman2.Models;
using Plugin.BLE.Abstractions.Contracts;

using Plugin.BLE;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Abstractions;
using System.Text;
using Plugin.BLE.Abstractions.EventArgs;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Net;
using System.Timers;
using System.Threading;
using Xamarin.Forms;

namespace Batman2.Services
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct BroadcastData
    {
        private Int16 _mV;
        private Int16 _mA;
        private Int32 _mW00;
        private Int32 _mWH00;

        public short mV { get => _mV; }
        public short mA { get => _mA; }
        public int mW00 { get => _mW00; }
        public int mWH00 { get => _mWH00; }
    }

    public class Battery : IBattery
    {
        private Guid SERVICE_UUID = new Guid("4fafc201-1fb5-459e-8fcc-c5c9c331914b");
        private Guid Update_UUID = new Guid("beb5483e-36e1-4688-b7f5-ea07361b26a8");
        private Guid Announce_UUID = new Guid("aa337802-6aca-42a5-b34d-a8d9fc12a266");

        private bool canScan, canUpdate, isConnected, isConnecting;
        private ObservableCollection<IDevice> deviceList = new ObservableCollection<IDevice>();
        private IDevice connectedDevice;
        private IBluetoothLE ble;
        private IAdapter adapter;
        private IReadOnlyList<IService> Services;
        private IService Service;

        private IReadOnlyList<ICharacteristic> Characteristics;
        private ICharacteristic Characteristic;
        private ICharacteristic Characteristic_update;
        private System.Timers.Timer _Reconnect_timer;
        private int BLE_Count = 0;
        CancellationTokenSource token = null;

        public Battery()
        {
            canScan = true;
            canUpdate = false;
            IsConnected = false;
            isConnecting = false;
            ble = CrossBluetoothLE.Current;
            adapter = CrossBluetoothLE.Current.Adapter;
            adapter.ScanMode = ScanMode.LowLatency;
            adapter.ScanTimeout = 1000; //1 seconds
            deviceList = new ObservableCollection<IDevice>();
            adapter.DeviceDiscovered += (s, a) =>
            {
                if ((a.Device.Name != null) && a.Device.Name.StartsWith("ESPBatMon"))
                //if (a.Device.Name != null)
                {
                    deviceList.Add(a.Device);
                    OnDeviceFound(a.Device);
                }
            };
            adapter.DeviceDisconnected += (o, arg) =>
            {
                Console.WriteLine("Device disconnected" + arg.Device.Id);
                Disconnect(false);
            };

            _Reconnect_timer = new System.Timers.Timer()
            {
                Interval =60000
            };
            _Reconnect_timer.Elapsed += OnCheckConnection;
            _Reconnect_timer.Enabled = false;
        }
        public bool ismock { get => false; }

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
        //private event EventHandler<DataEventArgs> _DataChanged;

        public event EventHandler<DataEventArgs> DataChanged;

        public event EventHandler<IDevice> DeviceFound;

        public event EventHandler<bool> Connection;

        protected virtual void OnDataChanged(DataEventArgs e)
        {
            BLE_Count++;
            Console.WriteLine("BLECount+=" + BLE_Count.ToString());
            DataChanged?.Invoke(this, e);
        }

        //protected void OnConnectTimeout(object sender, ElapsedEventArgs e)
        //{
        //    token.Cancel();
        //    Console.WriteLine("Connect Timeout");
        //    throw new Exception("Connection Timeout");
        //}

        public async void CheckConnection()
        {
            if (isConnecting)
            {
                Console.WriteLine("timer reconnecting, ignore manual trigger");
                return;
            }
            if (BLE_Count > 0)
            {
                Console.WriteLine("BLECount > 0");
                BLE_Count = 0;
                return;
            }
            Console.WriteLine("BLECount = 0, reconnect");
            _Reconnect_timer.Enabled = false;
            try
            {
                await ConnectAsync(connectedDevice);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Reconnect error-" + ex.Message);
            }
        }

        private void OnCheckConnection(object sender, ElapsedEventArgs e)
        {
            Task.Run(() => this.CheckConnection()).Wait();
        }

        protected virtual void OnDeviceFound(IDevice e)
        {
            DeviceFound?.Invoke(this, e);
        }

        private static T stamp<T>(byte[] bytes)
        {
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var structure = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return (T)structure;
        }

        public void Disconnect(bool cleanup)
        {
            if (isConnecting)
            {
                token.Cancel();
                Console.WriteLine("token cancelled");
            }
            if (cleanup && (connectedDevice != null))
            {
                connectedDevice.Dispose();
                connectedDevice = null;
                Console.WriteLine("connected Device Null");
            }
            if (IsConnected)
            {
                Service.Dispose();
                Characteristic.StopUpdatesAsync();
                Characteristic = null;
                Characteristic_update = null;
                Service = null;
                canUpdate = false;
                IsConnected = false;
                _Reconnect_timer.Enabled = false;
            }
        }

        public async Task<bool> ConnectAsync(IDevice item)
        {
            //if (ble.State == BluetoothState.Off)
            //{
            //    throw new Exception("Please turn on BlueTooth first!");
            //}
            if (ble.State != BluetoothState.On)
            {
                throw new Exception("BlueTooth unavailable!");
            }
            Disconnect(false);
            ConnectParameters cp = new ConnectParameters();
            isConnecting = true;
            token = new CancellationTokenSource();
            if (Device.RuntimePlatform == Device.iOS) {
                await adapter.ConnectToDeviceAsync(item, cp, token.Token);
            } else {
                await Device.InvokeOnMainThreadAsync(async () => {
                    await adapter.ConnectToDeviceAsync(item, cp, token.Token);
                });
            }
            isConnecting = false;
            connectedDevice = item;
            canUpdate = true;
            try
            {
                Services = await item.GetServicesAsync();
                Service = Services.FirstOrDefault(s => s.Id == SERVICE_UUID);
                if (Service == null) {
                    throw new Exception("Service UUID not found!");
                }
                Characteristics = await Service.GetCharacteristicsAsync();
                Characteristic_update = Characteristics[0];
                Characteristic = Characteristics[1];
            }
            catch (Exception ex)
            {
                Console.WriteLine("Service,Characteristics get error-" + ex.Message);
                Disconnect(false);
                _Reconnect_timer.Enabled = true;
                return false;
            }
            if (Service.Id != SERVICE_UUID ||
                Characteristic.Id != Announce_UUID ||
                Characteristic_update.Id != Update_UUID)
            {
                throw new Exception("Guid mismatch Error!");
            }

            Characteristic.ValueUpdated += (o, args) =>
            {
                Console.WriteLine($"BLE call {DateTime.Now.ToLongTimeString()}");
                var bytes = args.Characteristic.Value;
                var data = stamp<BroadcastData>(bytes);
                OnDataChanged(new DataEventArgs(data.mV / 1000.000,
                    data.mA / 1000.000,
                    data.mW00 / 100000.000000,
                    data.mWH00 / 100000.000000));
            };
            await Characteristic.StartUpdatesAsync();
            IsConnected = true;
            _Reconnect_timer.Enabled = true;
            return true;
        }

        public async Task<scanResult> ScanAndConnectAsyc()
        {
            if (ble.State != BluetoothState.On )
            {
                throw new Exception("Please turn on BlueTooth first!");
            }
            var result = scanResult.MultipleReturned;
            try
            {
                Disconnect(true);
                deviceList.Clear();
                if (!ble.Adapter.IsScanning)
                {
                    canScan = false;
                    await adapter.StartScanningForDevicesAsync();
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
            if (!IsConnected)
            {
                throw new Exception("Please connect first!");
            }
            //Int16 maxWH = Convert.ToInt16(value);
            var bytes = BitConverter.GetBytes(value);
            try
            {
                await Characteristic_update.WriteAsync(bytes);
            }
            catch (Exception ex)
            {
                throw new Exception("Write error", ex);
            }
            return true;
        }
    }
}
