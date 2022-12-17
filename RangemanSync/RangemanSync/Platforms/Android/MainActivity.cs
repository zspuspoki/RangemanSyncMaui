using Android.App;
using Android.Content;
using Android.Content.PM;
using RangemanSync.Platforms.Android;

namespace RangemanSync;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
    {
        base.OnActivityResult(requestCode, resultCode, data);

        if (resultCode == Result.Ok)
        {
            if (requestCode == ActivityRequestCode.SaveGPXFile)
            {
                using (System.IO.Stream stream = this.ContentResolver.OpenOutputStream(data.Data, "w"))
                {
                    using (var javaStream = new Java.IO.BufferedOutputStream(stream))
                    {
                        string gpx = Preferences.Default.Get(Constants.PrefKeyGPX, "");

                        if (!string.IsNullOrEmpty(gpx))
                        {
                            var gpxBytes = System.Text.Encoding.Unicode.GetBytes(gpx);
                            javaStream.Write(gpxBytes, 0, gpxBytes.Length);
                        }

                        javaStream.Flush();
                        javaStream.Close();
                    }
                }
            }
        }

    }
}
