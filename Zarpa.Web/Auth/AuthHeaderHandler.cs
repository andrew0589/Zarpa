using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components;

namespace Zarpa.Web.Auth
{
    // Web counterpart of the MAUI AuthHeaderHandler: attaches the signed-in user's
    // JWT to every API call, and turns a 401 (missing/expired token) into a clean
    // sign-out plus a return to the sign-in page.
    public class AuthHeaderHandler(WebAuthStateProvider auth, NavigationManager navigation) : DelegatingHandler
    {
        private readonly WebAuthStateProvider _auth = auth;
        private readonly NavigationManager _navigation = navigation;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Ensures localStorage was read after a hard page refresh (idempotent).
            await _auth.GetAuthenticationStateAsync();

            if (!string.IsNullOrWhiteSpace(_auth.Token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _auth.Token);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await _auth.SignoutAsync();
                _navigation.NavigateTo("signin");
            }

            return response;
        }
    }
}
