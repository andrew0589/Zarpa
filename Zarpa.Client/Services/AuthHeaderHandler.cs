using System.Net;
using System.Net.Http.Headers;
using Zarpa.Client.Utilities;

namespace Zarpa.Client.Services
{
    /// <summary>
    /// Attaches the signed-in user's JWT to every API call. Without it the API
    /// (which requires authentication on all but the auth endpoints) answers 401.
    /// A 401 means the token is missing or expired, so the session is cleared and the
    /// user is sent back to onboarding instead of seeing cryptic failures everywhere.
    /// </summary>
    public class AuthHeaderHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Resolved per call: the handler outlives sign-in/sign-out, and
            // AuthService is a singleton whose token changes over time.
            var authService = ServiceHelper.GetService<AuthService>();
            var token = authService?.Token;

            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized && authService is not null)
                await HandleExpiredSessionAsync(authService);

            return response;
        }

        private static Task HandleExpiredSessionAsync(AuthService authService)
        {
            // The callers of every in-flight request are about to hit their catch blocks
            // with this same 401 — keep their error snackbars off the onboarding page.
            UserMessageHelper.SuppressErrors(TimeSpan.FromSeconds(5));

            authService.Signout();
            ServiceHelper.GetService<UserSessionService>()?.Clear();

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await Shell.Current.GoToAsync("//OnboardingPage");
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
