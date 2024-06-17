using Microsoft.Extensions.Logging;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace BatMan2;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.RegisterServices()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

    private static MauiAppBuilder RegisterServices(this MauiAppBuilder builder) {
		builder.Services
			.AddSingleton<IReadingStore<Reading>, ReadingStore>()
			.AddSingleton<IBatManBattery, BatManBattery>()
			.AddSingleton<DeviceViewModel>()
            .AddSingleton<DevicePage>();
        return builder;
    }
}
