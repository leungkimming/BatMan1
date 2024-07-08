using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Compatibility.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace BatMan2;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
            .UseSkiaSharp()
            .RegisterServices()
            .UseMauiCompatibility()
            .ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			})
            .ConfigureMauiHandlers(handlers => {
#if IOS
                handlers.AddHandler<ContentPage, IOSContentPageHandler>();
#endif
            });

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

    private static MauiAppBuilder RegisterServices(this MauiAppBuilder builder) {
        builder.Services
            .AddSingleton<IReadingStore<Reading>, ReadingStore>()
            .AddSingleton<AnalysisViewModel>()
            .AddSingleton<AnalysisPage>()
            .AddSingleton<HistoryViewModel>()
            .AddSingleton<HistoryPage>()
            .AddSingleton<SetupViewModel>()
            .AddSingleton<SetupPage>()
            .AddSingleton<UpdateViewModel>()
            .AddSingleton<UpdatePage>()
            .AddSingleton<AboutPage>()
            .AddSingleton<DeviceViewModel>()
            .AddSingleton<DevicePage>()
			.AddSingleton<GaugeViewModel>()
			.AddSingleton<GaugePage>();
        return builder;
    }
}
