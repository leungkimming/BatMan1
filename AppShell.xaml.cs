namespace BatMan2;

public partial class AppShell : Shell {
    public AppShell() {
        InitializeComponent();

        Routing.RegisterRoute(nameof(GaugePage), typeof(GaugePage));
        Routing.RegisterRoute(nameof(AboutPage), typeof(AboutPage));
        Routing.RegisterRoute(nameof(AnalysisPage), typeof(AnalysisPage));
        Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
        Routing.RegisterRoute(nameof(SetupPage), typeof(SetupPage));
        Routing.RegisterRoute(nameof(UpdatePage), typeof(UpdatePage));
    }
}