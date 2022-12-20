using CommunityToolkit.Maui;
using RangemanSync.Services;
using RangemanSync.ViewModels.Download;
using Serilog;

namespace RangemanSync;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseMauiCommunityToolkit();
        builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

#if WINDOWS
		builder.Services.AddTransient<ISaveGPXFileService, RangemanSync.Platforms.Windows.SaveGPXFileService>();
#elif ANDROID
		builder.Services.AddTransient<ISaveGPXFileService, RangemanSync.Platforms.Android.SaveGPXFileService>();
#endif

        builder.Services.AddSingleton<BluetoothConnectorService>();
		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<MainPage>();

        builder.Logging.AddSerilog(dispose: true);

        return builder.Build();
	}
}
