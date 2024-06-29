namespace BatMan2;

public partial class App : Application
{
    public App(IServiceProvider provider)
	{
		InitializeComponent();
        IBatManBattery realBattery = new BatManBattery();
        DependencyService.RegisterSingleton<IBatManBattery>(realBattery);
        MainPage = new AppShell();
	}
}
