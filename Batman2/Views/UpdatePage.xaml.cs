using System;
using System.Collections.Generic;
using Batman2.ViewModels;
using Xamarin.Forms;
using Plugin.DeviceOrientation;
using Plugin.DeviceOrientation.Abstractions;

namespace Batman2.Views
{
    public partial class UpdatePage : ContentPage
    {
        UpdateViewModel _viewModel;

        public UpdatePage()
        {
            InitializeComponent();
            BindingContext = _viewModel = new UpdateViewModel();
            CrossDeviceOrientation.Current.UnlockOrientation();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.OnAppearing();
        }
    }
}
