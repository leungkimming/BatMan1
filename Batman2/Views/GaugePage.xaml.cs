
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Xamarin.Forms;
using Batman2.Controls;
using Batman2.ViewModels;

namespace Batman2.Views
{
    public partial class GaugePage : ContentPage
    {
        private Timer _timer;
        private Random RAND = new Random();
        GaugeViewModel _viewModel;

        public GaugePage()
        {
            InitializeComponent();
            _viewModel = DependencyService.Get<GaugeViewModel>();
            BindingContext = _viewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.OnAppearing();
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
        }
    }
}
