using CommunityToolkit.Maui;
using MetroLog.MicrosoftExtensions;
using MetroLog.Operators;
using Microsoft.Extensions.Logging;
using RangemanSync.Services;
using RangemanSync.Services.Common;
using RangemanSync.Services.DeviceLocation;
using RangemanSync.ViewModels.Config;
using RangemanSync.ViewModels.Download;
using RangemanSync.ViewModels.Map;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace RangemanSync;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseMauiCommunityToolkit();
        builder
			.UseMauiApp<App>()
            .UseSkiaSharp(true)
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

        builder.Services.AddSingleton<ILocationService, LocationService>();
        builder.Services.AddSingleton<IWatchControllerUtilities, WatchControllerUtilities>();
        builder.Services.AddSingleton<BluetoothConnectorService>();
        builder.Services.AddSingleton<ProgressMessagesService>();
		builder.Services.AddSingleton<MainPageViewModel>();
		builder.Services.AddSingleton<MainPage>();
        builder.Services.AddSingleton<ConfigPageViewModel>();
        builder.Services.AddSingleton<ConfigPage>();
        builder.Services.AddSingleton<NodesViewModel>();

        builder.Services.AddSingleton<MapPageViewModel>();
        builder.Services.AddSingleton<MapPage>((sp) => 
        {
            var logger = sp.GetRequiredService<ILogger<MapPage>>();
            var locationService = sp.GetRequiredService<ILocationService>();
            var mapPage = new MapPage(logger, locationService);
            var mapPageViewModel = sp.GetRequiredService<MapPageViewModel>();
            mapPageViewModel.MapPageView = mapPage;
            mapPage.BindingContext = mapPageViewModel;
            return mapPage;
        });

        builder.Services.AddSingleton<IMapPageView, MapPage>((sp) =>
        {
            return sp.GetRequiredService<MapPage>();
        });

        MauiExceptions.UnhandledException += MauiExceptions_UnhandledException ;

        return builder.Build();
	}

    private static string GetUnhandledExceptionLogPath()
    {
        const string errorFileName = "Fatal.log";
        var libraryPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal); // iOS: Environment.SpecialFolder.Resources
        return Path.Combine(libraryPath, errorFileName);
    }

    private static void LogUnhandledException(Exception exception)
    {
        try
        {
            if (exception != null)
            {
                var errorFilePath = GetUnhandledExceptionLogPath();
                var errorMessage = string.Format("Time: {0}\r\nError: Unhandled Exception\r\n{1}",
                DateTime.Now, exception.ToString());
                File.WriteAllText(errorFilePath, errorMessage);
            }
        }
        catch
        {
            // just suppress any error logging exceptions
        }
    }

    private static void MauiExceptions_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogUnhandledException(e.ExceptionObject as Exception);
        Application.Current.MainPage.DisplayAlert("Error", $"An unexpected error occured during app execution. Sorry for the inconvenience. \nThe log file was saved here: {GetUnhandledExceptionLogPath()}", "OK");
    }
}
