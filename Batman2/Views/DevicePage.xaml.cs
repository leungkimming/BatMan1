using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Batman2.Models;
using Batman2.Views;
using Batman2.ViewModels;
using Plugin.DeviceOrientation;
using Plugin.DeviceOrientation.Abstractions;

namespace Batman2.Views
{
    public partial class DevicePage : ContentPage
    {
        DeviceViewModel _viewModel;

        public DevicePage()
        {
            InitializeComponent();
            BindingContext = _viewModel = new DeviceViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            if (Device.RuntimePlatform == Device.iOS) {
                CrossDeviceOrientation.Current.UnlockOrientation();
            }
            _viewModel.OnAppearing();
        }
    }
}
