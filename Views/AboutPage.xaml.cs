namespace BatMan2.Views;

public partial class AboutPage : ContentPage {
    public AboutPage() {
        InitializeComponent();
        Title = "About BatMan";
        proteoWebView.Navigating += OnNavigating;
    }
    private async void OnNavigating(object sender, WebNavigatingEventArgs e) {
        if (e.Url.StartsWith("https://github.com")) {
            e.Cancel = true;
            await Launcher.OpenAsync(e.Url);
        }
    }
}
