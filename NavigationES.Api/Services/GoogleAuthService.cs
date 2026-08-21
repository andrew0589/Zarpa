using Google.Apis.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text.Json;

namespace NavigationES.Api.Services
{
    // OAuth mechanics with Google: builds the authorize URL, exchanges the code for tokens
    // and validates the ID token. The mobile app never talks to Google directly — this API
    // is the OAuth "Web application" client, so the client secret stays server-side.
    public class GoogleAuthService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
    {
        public const string ProviderName = "Google";

        private const string AuthorizeEndpoint = "https://accounts.google.com/o/oauth2/v2/auth";
        private const string TokenEndpoint = "https://oauth2.googleapis.com/token";
        private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);

        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _cache = cache;

        private string ClientId => _configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientId is not configured.");

        private string ClientSecret => _configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientSecret is not configured.");

        public string CreateAuthorizationUrl(string redirectUri, string client)
        {
            // Random state, remembered for one round-trip: the callback must present it back,
            // otherwise the code was not produced by a flow we started (CSRF protection).
            // The cached value records which client (app/web) started the flow.
            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            _cache.Set(StateCacheKey(state), client, StateLifetime);

            var query = new Dictionary<string, string?>
            {
                ["client_id"] = ClientId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = "openid email profile",
                ["state"] = state,
                ["prompt"] = "select_account"
            };

            return QueryHelpers.AddQueryString(AuthorizeEndpoint, query);
        }

        // Returns the client (app/web) that started the flow, or null when the state
        // is unknown — i.e. the code was not produced by a flow we started.
        public string? ValidateState(string? state)
        {
            if (string.IsNullOrEmpty(state)) return null;

            var key = StateCacheKey(state);
            if (!_cache.TryGetValue(key, out string? client)) return null;

            _cache.Remove(key); // single use
            return client ?? SocialAuth.AppClient;
        }

        // Exchanges the authorization code for tokens and returns the validated ID token
        // payload (sub/email/name), or null if the exchange or validation fails.
        public async Task<GoogleJsonWebSignature.Payload?> ExchangeCodeAsync(string code, string redirectUri)
        {
            try
            {
                var response = await _httpClient.PostAsync(TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = ClientId,
                    ["client_secret"] = ClientSecret,
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code"
                }));

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Google token exchange failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
                    return null;
                }

                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var idToken = json.RootElement.GetProperty("id_token").GetString();

                // Verifies the signature against Google's public keys (JWKS), plus issuer,
                // expiry and audience — this is what replaces the password/hash comparison.
                return await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [ClientId]
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Google auth failed: {ex.Message}");
                return null;
            }
        }

        private static string StateCacheKey(string state) => $"google-oauth-state:{state}";
    }
}
