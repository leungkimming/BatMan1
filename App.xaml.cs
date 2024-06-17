namespace BatMan2;

public partial class App : Application
{
    private static IServiceProvider ServicesProvider;
    public static IServiceProvider Services => ServicesProvider;
    public App(IServiceProvider provider)
	{
		ServicesProvider = provider;
		InitializeComponent();
        //batmanBattery = Services.GetService<IBatManBattery>();
        MainPage = new AppShell();
	}
}
