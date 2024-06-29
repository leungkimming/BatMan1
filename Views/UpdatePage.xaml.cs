namespace BatMan2.Views
{
    public partial class UpdatePage : ContentPage
    {
        UpdateViewModel _viewModel;

        public UpdatePage(UpdateViewModel vm)
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
