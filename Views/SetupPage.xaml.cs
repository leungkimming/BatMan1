namespace BatMan2.Views
{
    public partial class SetupPage : ContentPage
    {
        SetupViewModel _viewModel;
        public SetupPage(SetupViewModel vm)
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
