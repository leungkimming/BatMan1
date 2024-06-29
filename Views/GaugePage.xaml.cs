using System.Timers;

namespace BatMan2.Views
{
    public partial class GaugePage : ContentPage
    {
        //private Timer _timer;
        //private Random RAND = new Random();
        GaugeViewModel _viewModel;

        public GaugePage(GaugeViewModel viewModel)
        {
            InitializeComponent();
            BindingContext = _viewModel = viewModel;
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
