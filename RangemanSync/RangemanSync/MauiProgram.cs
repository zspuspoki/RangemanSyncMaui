using CommunityToolkit.Maui;
using MetroLog.MicrosoftExtensions;
using MetroLog.Operators;
using Microsoft.Extensions.Logging;
using RangemanSync.Services;
using RangemanSync.ViewModels.Config;
using RangemanSync.ViewModels.Download;

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

        builder.Logging
#if DEBUG
            .AddTraceLogger(
                options =>
                {
                    options.MinLevel = LogLevel.Trace;
                    options.MaxLevel = LogLevel.Critical;
                }) // Will write to the Debug Output
#endif
            .AddInMemoryLogger(
                options =>
                {
                    options.MaxLines = 1024;
                    options.MinLevel = LogLevel.Debug;
                    options.MaxLevel = LogLevel.Critical;
                })
#if RELEASE
            .AddStreamingFileLogger(
                options =>
                {
                    options.RetainDays = 2;
                    options.FolderPath = Path.Combine(
                        FileSystem.CacheDirectory,
                        "MetroLogs");
                })
#endif
            .AddConsoleLogger(
                options =>
                {
                    options.MinLevel = LogLevel.Information;
                    options.MaxLevel = LogLevel.Critical;
                }); // Will write to the Console Output (logcat for android)

        builder.Services.AddSingleton(LogOperatorRetriever.Instance);

#if WINDOWS
        builder.Services.AddTransient<ISaveTextFileService, RangemanSync.Platforms.Windows.SaveFileService>();
#elif ANDROID
		builder.Services.AddTransient<ISaveTextFileService, RangemanSync.Platforms.Android.SaveFileService>();
#endif

        builder.Services.AddSingleton<BluetoothConnectorService>();
		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<ConfigPageViewModel>();
        builder.Services.AddSingleton<ConfigPage>();

        return builder.Build();
	}
}
