using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Zarpa.Client
{
    // Catches the zarpa:// redirect at the end of the social-login browser flow
    // and hands the callback URL back to WebAuthenticator.
    [Activity(NoHistory = true, LaunchMode = LaunchMode.SingleTop, Exported = true)]
    [IntentFilter(new[] { Intent.ActionView },
        Categories = new[] { Intent.CategoryDefault, Intent.CategoryBrowsable },
        DataScheme = CallbackScheme)]
    public class WebAuthenticationCallbackActivity : Microsoft.Maui.Authentication.WebAuthenticatorCallbackActivity
    {
        const string CallbackScheme = "zarpa";
    }
}
