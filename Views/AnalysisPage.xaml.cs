namespace BatMan2.Views
{
    public partial class AnalysisPage : ContentPage
    {
        AnalysisViewModel _viewModel;

        public AnalysisPage(AnalysisViewModel vm)
        {
            InitializeComponent();

            BindingContext = _viewModel = vm;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel.OnAppearing();
        }
    }
}
