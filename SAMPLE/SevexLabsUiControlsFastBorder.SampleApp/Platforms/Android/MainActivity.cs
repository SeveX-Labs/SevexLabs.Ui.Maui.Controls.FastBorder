using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Java.Util.Logging;

namespace SevexLabsUiControlsFastBorder.SampleApp
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        public override void OnLowMemory()
        {
            System.Diagnostics.Debug.WriteLine($"$$$$$$$ !!!!! LowMemory !!!!! $$$$$$$$");

            base.OnLowMemory();
        }

        public override void OnTrimMemory([GeneratedEnum] TrimMemory level)
        {
            System.Diagnostics.Debug.WriteLine($"$$$$$$$ TrimMemory: {level} $$$$$$$$");

            base.OnTrimMemory(level);
        }
    }
}
