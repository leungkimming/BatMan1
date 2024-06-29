namespace BatMan2.Views;

public partial class DevicePage : ContentPage
{
    DeviceViewModel _viewModel;

    public DevicePage(DeviceViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        //if (DeviceInfo.Current.Platform == DevicePlatform.iOS) {
        //    CrossDeviceOrientation.Current.UnlockOrientation();
        //}
        _viewModel.OnAppearing();
    }
}

