using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

using Batman2.Models;
using Batman2.Views;
using Batman2.ViewModels;
using Plugin.DeviceOrientation;
using Plugin.DeviceOrientation.Abstractions;

namespace Batman2.Views
{
    public partial class AnalysisPage : ContentPage
    {
        AnalysisViewModel _viewModel;

        public AnalysisPage()
        {
            InitializeComponent();

            BindingContext = _viewModel = new AnalysisViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.OnAppearing();
            if (Device.RuntimePlatform == Device.iOS) {
                CrossDeviceOrientation.Current.UnlockOrientation();
            }
        }
    }
}
