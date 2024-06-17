using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;
using BatMan2.Models;

namespace BatMan2.Services
{
    public interface IBatManBattery
    {
        bool CanScan { get; }
        bool CanUpdate { get; }
        bool ismock { get; }
        bool IsConnected { get; set; }

        ObservableCollection<IDevice> DeviceList { get; }
        IDevice ConnectedDevice { get; }

        Task<scanResult> ScanAndConnectAsyc();
        Task<bool> ConnectAsync(IDevice item);
        Task<bool> UpdateAsync(Int16 value);
        void Disconnect(bool cleanup);
        void CheckConnection();

        event EventHandler<DataEventArgs> DataChanged;
        event EventHandler<IDevice> DeviceFound;
        event EventHandler<bool> Connection;
    }
}
