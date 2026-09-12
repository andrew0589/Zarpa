using System.Net;
using System.Net.Http.Headers;
using NavigationES.Client.Utilities;

namespace NavigationES.Client.Services
{
    /// <summary>
    /// Attaches the signed-in user's JWT to every API call. Without it the API
    /// (which requires authentication on all but the auth endpoints) answers 401.
    /// A 401 means the token is missing or expired, so the session is cleared and the
    /// user is sent back to sign-in instead of seeing cryptic failures everywhere.
    /// </summary>
    public class AuthHeaderHandler : DelegatingHandler
    {
        // Values the API knows (UserActivityMiddleware); anything else is filed as "app".
        private static readonly string ClientName =
            DeviceInfo.Current.Platform == DevicePlatform.Android ? "android"
            : DeviceInfo.Current.Platform == DevicePlatform.iOS ? "ios"
            : DeviceInfo.Current.Platform == DevicePlatform.WinUI ? "windows"
            : "app";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Resolved per call: the handler outlives sign-in/sign-out, and
            // AuthService is a singleton whose token changes over time.
            var authService = ServiceHelper.GetService<AuthService>();
            var token = authService?.Token;

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Tells the API which client is calling — recorded as the user's "last used
            // from" by its UserActivityMiddleware (the website sends "web").
            request.Headers.TryAddWithoutValidation("X-Client", ClientName);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized && authService is not null)
                await HandleExpiredSessionAsync(authService);

            return response;
        }

        private static Task HandleExpiredSessionAsync(AuthService authService)
        {
            // The callers of every in-flight request are about to hit their catch blocks
            // with this same 401 — keep their error snackbars off the sign-in page.
            UserMessageHelper.SuppressErrors(TimeSpan.FromSeconds(5));

            authService.Signout();
            ServiceHelper.GetService<UserSessionService>()?.Clear();
            ServiceHelper.GetService<SelectedLicenseService>()?.Clear();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Shell.Current.GoToAsync("//SigninPage");
                }
                catch
                {
                    // best-effort navigation
                }
            });

            return Task.CompletedTask;
        }
    }
}
