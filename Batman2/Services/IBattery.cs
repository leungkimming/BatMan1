using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Batman2.Models;
using Plugin.BLE.Abstractions.Contracts;
using System.Collections.ObjectModel;

namespace Batman2.Services
{
    public interface IBattery
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

        //Task<bool> UpdateItemAsync(T item);
        //Task<bool> DeleteItemAsync(string id);
        //Task<T> GetItemAsync(string id);
        //Task<IEnumerable<T>> GetItemsAsync(bool forceRefresh = false);
    }
}
