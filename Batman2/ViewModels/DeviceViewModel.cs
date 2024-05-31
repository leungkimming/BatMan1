using Plugin.BLE.Abstractions.Contracts;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Batman2.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using Xamarin.Forms;
using Batman2.Views;
using Batman2.Services;

namespace Batman2.ViewModels
{
    public class DeviceViewModel : BaseViewModel
    {
        private IDevice _selectedItem;
        private string _error;
        private bool _canDemo;
        private bool _isConnecting = false;

        public ObservableCollection<IDevice> Items { get; set; }
        public Command LoadItemsCommand { get; }
        public Command<IDevice> ItemTapped { get; }
        public Command HistoryCommand { get; }
        public Command AnalyzeCommand { get; }
        public Command DemoCommand { get; }
        public Command AboutCommand { get; }

        public string Error
        {
            get { return _error; }
            set { SetProperty(ref _error, value); }
        }

        public bool canDemo
        {
            get { return _canDemo; }
            set { SetProperty(ref _canDemo, value); }
        }

        public bool isConnecting
        {
            get { return _isConnecting; }
            set { SetProperty(ref _isConnecting, value); }
        }

        public DeviceViewModel()
        {
            Items = new ObservableCollection<IDevice>();
            Title = "Scan & Connect Battery";
            canDemo = true;
            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());
            ItemTapped = new Command<IDevice>(OnItemSelected);
            //DemoCommand = new Command(OnDemo);
            DemoCommand = new Command(
                execute: () =>
                {
                    OnDemo(this);
                },
                canExecute: () =>
                {
                    return canDemo;
                });
            Battery.DeviceFound += (o, device) =>
            {
                Items.Add(device);
            };
            HistoryCommand = new Command(OnViewHistory);
            AnalyzeCommand = new Command(OnAnalyze);
            AboutCommand = new Command(OnAbout);
        }

        void RefreshCanExecutes()
        {
            (DemoCommand as Command).ChangeCanExecute();
        }

        public void OnAppearing()
        {
            Battery.Disconnect(true);
        }

        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;
            _selectedItem = null;
            Error = "";
            try
            {
                Items.Clear();
                scanResult result = await Battery.ScanAndConnectAsyc();
                if (result == scanResult.SingleConected)
                {
                    SelectedItem = Battery.ConnectedDevice;
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                Error = message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public IDevice SelectedItem
        {
            get => _selectedItem;
            set
            {
                SetProperty(ref _selectedItem, value);
                OnItemSelected(value);
            }
        }

        async void OnItemSelected(IDevice item)
        {
            if (item == null || isConnecting)
                return;
            //IsBusy = true;
            Error = "";
            isConnecting = true;
            if (Battery.ConnectedDevice != item)
            {
                try
                {
                    await Battery.ConnectAsync(item);
                } catch (Exception ex)
                {
                    string message = ex.Message;
                    Error = message;
                    isConnecting = false;
                    return;
                }
            }
            //IsBusy = false;
            isConnecting = false;
            // This will push the BatteryPage onto the navigation stack
            await Shell.Current.GoToAsync($"{nameof(GaugePage)}?{nameof(GaugeViewModel)}");
        }

        private void OnDemo(object obj)
        {
            Items.Clear();
            IBattery mockBattery = new MockBattery();
            DependencyService.RegisterSingleton<IBattery>(mockBattery);
            Error = "Demo mode activated!";
            canDemo = false;
            RefreshCanExecutes();
            DependencyService.Get<GaugeViewModel>().ActivateDemo();
            Battery.DeviceFound += (o, device) =>
            {
                Items.Add(device);
            };
        }

        private async void OnViewHistory(object obj)
        {
            await Shell.Current.GoToAsync(nameof(HistoryPage));
        }

        private async void OnAbout(object obj)
        {
            await Shell.Current.GoToAsync(nameof(AboutPage));
        }

        private async void OnAnalyze(object obj)
        {
            await Shell.Current.GoToAsync($"{ nameof(AnalysisPage)}?BatteryName=");
        }
    }
}
