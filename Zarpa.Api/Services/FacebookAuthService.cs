using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Zarpa.Api.Services
{
    // OAuth mechanics with Facebook. Mirrors GoogleAuthService with Facebook's twist: the
    // web flow returns no OIDC id_token, only an access token — identity comes from calling
    // the Graph API /me endpoint with it. Trust rests on the code exchange happening
    // server-to-server with the app secret, so no signature validation is needed.
    public class FacebookAuthService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
    {
        public const string ProviderName = "Facebook";

        private const string AuthorizeEndpoint = "https://www.facebook.com/v23.0/dialog/oauth";
        private const string TokenEndpoint = "https://graph.facebook.com/v23.0/oauth/access_token";
        private const string MeEndpoint = "https://graph.facebook.com/v23.0/me";
        private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _cache = cache;

        private string AppId => _configuration["Authentication:Facebook:AppId"]
            ?? throw new InvalidOperationException("Authentication:Facebook:AppId is not configured.");

        private string AppSecret => _configuration["Authentication:Facebook:AppSecret"]
            ?? throw new InvalidOperationException("Authentication:Facebook:AppSecret is not configured.");

        public string CreateAuthorizationUrl(string redirectUri)
        {
            // Random state, remembered for one round-trip: the callback must present it back,
            // otherwise the code was not produced by a flow we started (CSRF protection).
            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            _cache.Set(StateCacheKey(state), true, StateLifetime);

            var query = new Dictionary<string, string?>
            {
                ["client_id"] = AppId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                // Facebook separates scopes with commas, not spaces.
                ["scope"] = "email,public_profile",
                ["state"] = state
            };

            return QueryHelpers.AddQueryString(AuthorizeEndpoint, query);
        }

        public bool ValidateState(string? state)
        {
            if (string.IsNullOrEmpty(state)) return false;

            var key = StateCacheKey(state);
            if (!_cache.TryGetValue(key, out _)) return false;

            _cache.Remove(key); // single use
            return true;
        }

        // Exchanges the authorization code for an access token and fetches the user's
        // identity from Graph /me, or null if any step fails. Email can legitimately be
        // empty: the user may deny the email permission or have a phone-only account.
        public async Task<Payload?> ExchangeCodeAsync(string code, string redirectUri)
        {
            try
            {
                var tokenResponse = await _httpClient.GetAsync(QueryHelpers.AddQueryString(TokenEndpoint, new Dictionary<string, string?>
                {
                    ["code"] = code,
                    ["client_id"] = AppId,
                    ["client_secret"] = AppSecret,
                    ["redirect_uri"] = redirectUri
                }));

                if (!tokenResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Facebook token exchange failed: {tokenResponse.StatusCode} {await tokenResponse.Content.ReadAsStringAsync()}");
                    return null;
                }

                string? accessToken;
                using (var json = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync()))
                    accessToken = json.RootElement.GetProperty("access_token").GetString();

                if (string.IsNullOrEmpty(accessToken)) return null;

                var meResponse = await _httpClient.GetAsync(QueryHelpers.AddQueryString(MeEndpoint, new Dictionary<string, string?>
                {
                    ["fields"] = "id,name,email",
                    ["access_token"] = accessToken,
                    // Proves to Facebook that the call comes from the server holding the app
                    // secret, so a leaked access token alone cannot replay it.
                    ["appsecret_proof"] = AppSecretProof(accessToken)
                }));

                if (!meResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Facebook /me failed: {meResponse.StatusCode} {await meResponse.Content.ReadAsStringAsync()}");
                    return null;
                }

                using var me = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
                var id = me.RootElement.GetProperty("id").GetString();
                if (string.IsNullOrEmpty(id)) return null;

                var name = me.RootElement.TryGetProperty("name", out var n) ? n.GetString() : null;
                var email = me.RootElement.TryGetProperty("email", out var e) ? e.GetString() : null;

                return new Payload(id, name, email ?? string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Facebook auth failed: {ex.Message}");
                return null;
            }
        }

        private string AppSecretProof(string accessToken) => Convert.ToHexString(
            HMACSHA256.HashData(Encoding.UTF8.GetBytes(AppSecret), Encoding.UTF8.GetBytes(accessToken)))
            .ToLowerInvariant();

        private static string StateCacheKey(string state) => $"facebook-oauth-state:{state}";

        public sealed record Payload(string Subject, string? Name, string Email);
    }
}
