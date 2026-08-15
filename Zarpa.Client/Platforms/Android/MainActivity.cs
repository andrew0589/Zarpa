using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace Zarpa.Client;

// AdjustResize: with the default adjustPan the window just pans when the keyboard opens,
// so the auth pages' ScrollView cannot reach the content below the focused field.
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, WindowSoftInputMode = SoftInput.AdjustResize, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
}
