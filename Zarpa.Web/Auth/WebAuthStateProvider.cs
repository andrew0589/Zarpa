using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using Zarpa.Shared.Dtos;

namespace Zarpa.Web.Auth
{
    // The web counterpart of the MAUI AuthService: keeps the signed-in user and JWT,
    // persists them in the browser's localStorage (the web's "Preferences"), and
    // exposes the session to Blazor's [Authorize]/<AuthorizeView> infrastructure.
    public class WebAuthStateProvider(IJSRuntime js) : AuthenticationStateProvider
    {
        private const string StorageKey = "zarpa-auth";
        private readonly IJSRuntime _js = js;
        private bool _initialized;

        public LoggedInUser? User { get; private set; }
        public string? Token { get; private set; }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            // First call after a page load restores the session from localStorage.
            if (!_initialized)
            {
                await LoadFromStorageAsync();
                _initialized = true;
            }

            return BuildState();
        }

        public async Task SigninAsync(AuthResponseDto auth)
        {
            (User, Token) = auth;
            _initialized = true;

            await _js.InvokeVoidAsync("localStorage.setItem", StorageKey, JsonSerializer.Serialize(auth));
            NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
        }

        public async Task SignoutAsync()
        {
            (User, Token) = (null, null);
            _initialized = true;

            await _js.InvokeVoidAsync("localStorage.removeItem", StorageKey);
            NotifyAuthenticationStateChanged(Task.FromResult(BuildState()));
        }

        private async Task LoadFromStorageAsync()
        {
            try
            {
                var serialized = await _js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
                if (string.IsNullOrWhiteSpace(serialized)) return;

                var auth = JsonSerializer.Deserialize<AuthResponseDto>(serialized);
                if (auth is null || IsExpired(auth.Token)) return;

                (User, Token) = auth;
            }
            catch
            {
                // Corrupt storage — treat as signed out.
            }
        }

        // Reads the JWT exp claim without validating the signature — the API validates
        // every call anyway; this only avoids restoring a session that would 401
        // on its first request.
        private static bool IsExpired(string token)
        {
            try
            {
                var parts = token.Split('.');
                if (parts.Length != 3) return true;

                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                using var json = JsonDocument.Parse(Convert.FromBase64String(payload));

                var exp = json.RootElement.GetProperty("exp").GetInt64();
                return DateTimeOffset.FromUnixTimeSeconds(exp) <= DateTimeOffset.UtcNow;
            }
            catch
            {
                return true;
            }
        }

        private AuthenticationState BuildState()
        {
            if (User is null || string.IsNullOrWhiteSpace(Token))
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

            var identity = new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, User.Id.ToString()),
                    new Claim(ClaimTypes.Name, User.Name),
                    new Claim(ClaimTypes.Email, User.Email),
                ],
                authenticationType: "zarpa");

            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
    }
}
