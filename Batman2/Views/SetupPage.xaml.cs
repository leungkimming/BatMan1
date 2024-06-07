using System;
using System.Collections.Generic;
using Batman2.ViewModels;
using Xamarin.Forms;
using Plugin.DeviceOrientation;
using Plugin.DeviceOrientation.Abstractions;

namespace Batman2.Views
{
    public partial class SetupPage : ContentPage
    {
        SetupViewModel _viewModel;
        public SetupPage()
        {
            InitializeComponent();
            BindingContext = _viewModel = new SetupViewModel();
            if (Device.RuntimePlatform == Device.iOS) {
                CrossDeviceOrientation.Current.UnlockOrientation();
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.OnAppearing();
        }
    }
}
