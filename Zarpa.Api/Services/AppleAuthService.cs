using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using System.Text.Json;

namespace Zarpa.Api.Services
{
    // OAuth mechanics with Apple ("Sign in with Apple", web flow). Mirrors GoogleAuthService
    // with Apple's twists: there is no static client secret — we mint a short-lived ES256 JWT
    // signed with the .p8 key from the developer portal — and because we request the
    // name/email scopes, Apple POSTs the callback (form_post) instead of a GET redirect.
    public class AppleAuthService(HttpClient httpClient, IConfiguration configuration, IMemoryCache cache)
    {
        public const string ProviderName = "Apple";

        private const string AuthorizeEndpoint = "https://appleid.apple.com/auth/authorize";
        private const string TokenEndpoint = "https://appleid.apple.com/auth/token";
        private const string RevokeEndpoint = "https://appleid.apple.com/auth/revoke";
        private const string JwksEndpoint = "https://appleid.apple.com/auth/keys";
        private const string AppleIssuer = "https://appleid.apple.com";
        private static readonly TimeSpan StateLifetime = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan ClientSecretCacheLifetime = TimeSpan.FromDays(1);
        private static readonly TimeSpan JwksCacheLifetime = TimeSpan.FromHours(12);

        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _cache = cache;

        // The Services ID acts as the OAuth client_id in the web flow (the bundle ID would be
        // the audience only for the native flow).
        private string ServicesId => Require("ServicesId");
        private string TeamId => Require("TeamId");
        private string KeyId => Require("KeyId");
        private string PrivateKey => Require("PrivateKey");

        private string Require(string key) => _configuration[$"Authentication:Apple:{key}"]
            ?? throw new InvalidOperationException($"Authentication:Apple:{key} is not configured.");

        public string CreateAuthorizationUrl(string redirectUri, string client)
        {
            // Random state, remembered for one round-trip: the callback must present it back,
            // otherwise the code was not produced by a flow we started (CSRF protection).
            // The cached value records which client (app/web) started the flow.
            var state = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
            _cache.Set(StateCacheKey(state), client, StateLifetime);

            var query = new Dictionary<string, string?>
            {
                ["client_id"] = ServicesId,
                ["redirect_uri"] = redirectUri,
                ["response_type"] = "code",
                ["scope"] = "name email",
                // Mandatory whenever the name/email scopes are requested — this is why the
                // callback endpoint is a POST reading form fields.
                ["response_mode"] = "form_post",
                ["state"] = state
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
        // payload (sub/email), or null if the exchange or validation fails.
        public async Task<Payload?> ExchangeCodeAsync(string code, string redirectUri)
        {
            try
            {
                var response = await _httpClient.PostAsync(TokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["code"] = code,
                    ["client_id"] = ServicesId,
                    ["client_secret"] = CreateClientSecret(),
                    ["redirect_uri"] = redirectUri,
                    ["grant_type"] = "authorization_code"
                }));

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Apple token exchange failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
                    return null;
                }

                using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                var idToken = json.RootElement.GetProperty("id_token").GetString();

                // Apple hands out the refresh token only here. It is kept so the grant can be
                // revoked at account deletion (Guideline 5.1.1(v)); it is never used to refresh.
                var refreshToken = json.RootElement.TryGetProperty("refresh_token", out var rt)
                    ? rt.GetString()
                    : null;

                // Verifies the signature against Apple's public keys (JWKS), plus issuer,
                // expiry and audience — this is what replaces the password/hash comparison.
                var validation = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters
                {
                    ValidIssuer = AppleIssuer,
                    ValidAudience = ServicesId,
                    IssuerSigningKeys = (await GetJwksAsync()).GetSigningKeys()
                });

                if (!validation.IsValid)
                {
                    Console.WriteLine($"Apple ID token validation failed: {validation.Exception?.Message}");
                    return null;
                }

                validation.Claims.TryGetValue(JwtRegisteredClaimNames.Sub, out var sub);
                validation.Claims.TryGetValue(JwtRegisteredClaimNames.Email, out var email);
                validation.Claims.TryGetValue("email_verified", out var emailVerified);

                if (sub is not string subject || string.IsNullOrEmpty(subject)) return null;

                return new Payload(subject, email as string ?? string.Empty, IsTrue(emailVerified), refreshToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Apple auth failed: {ex.Message}");
                return null;
            }
        }

        // Revokes the Apple grant so a deleted account behaves like a first-time sign-in if
        // the person ever returns. Best-effort by design: a failure here must never block
        // the caller. (Kept for the future account-deletion flow.)
        public async Task<bool> TryRevokeRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken)) return false;

            try
            {
                var response = await _httpClient.PostAsync(RevokeEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = ServicesId,
                    ["client_secret"] = CreateClientSecret(),
                    ["token"] = refreshToken,
                    ["token_type_hint"] = "refresh_token"
                }));

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Apple token revoke failed: {response.StatusCode} {await response.Content.ReadAsStringAsync()}");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Apple token revoke failed: {ex.Message}");
                return false;
            }
        }

        // Apple sends the user's name only on the very first authorization, as a JSON "user"
        // form field — it never appears in the ID token, so the callback is the only chance
        // to capture it. Shape: {"name":{"firstName":"...","lastName":"..."},"email":"..."}
        public static string? ParseUserName(string? userJson)
        {
            if (string.IsNullOrWhiteSpace(userJson)) return null;

            try
            {
                using var json = JsonDocument.Parse(userJson);
                if (!json.RootElement.TryGetProperty("name", out var name)) return null;

                var first = name.TryGetProperty("firstName", out var f) && f.ValueKind == JsonValueKind.String ? f.GetString() : null;
                var last = name.TryGetProperty("lastName", out var l) && l.ValueKind == JsonValueKind.String ? l.GetString() : null;

                var full = string.Join(' ', new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
                return string.IsNullOrWhiteSpace(full) ? null : full;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // The client secret is a JWT we sign ourselves with the .p8 key (Apple allows up to
        // 6 months of validity; we keep it short and cache it instead).
        private string CreateClientSecret()
        {
            return _cache.GetOrCreate("apple-client-secret", entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = ClientSecretCacheLifetime;

                using var ecdsa = ECDsa.Create();
                ecdsa.ImportPkcs8PrivateKey(DecodePrivateKey(PrivateKey), out _);

                var now = DateTime.UtcNow;
                return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
                {
                    Issuer = TeamId,
                    Audience = AppleIssuer,
                    Claims = new Dictionary<string, object> { [JwtRegisteredClaimNames.Sub] = ServicesId },
                    IssuedAt = now,
                    NotBefore = now,
                    // Comfortably longer than the cache lifetime so a cached secret never expires mid-use.
                    Expires = now.AddDays(7),
                    SigningCredentials = new SigningCredentials(
                        new ECDsaSecurityKey(ecdsa) { KeyId = KeyId }, SecurityAlgorithms.EcdsaSha256)
                });
            })!;
        }

        private async Task<JsonWebKeySet> GetJwksAsync()
        {
            return (await _cache.GetOrCreateAsync("apple-jwks", async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = JwksCacheLifetime;
                return new JsonWebKeySet(await _httpClient.GetStringAsync(JwksEndpoint));
            }))!;
        }

        // Accepts the PEM contents of the .p8 file; env vars often flatten newlines, so strip
        // the armor lines and whitespace and decode what remains.
        private static byte[] DecodePrivateKey(string pem) => Convert.FromBase64String(pem
            .Replace("-----BEGIN PRIVATE KEY-----", "")
            .Replace("-----END PRIVATE KEY-----", "")
            .Replace("\\n", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Trim());

        private static bool IsTrue(object? claimValue) =>
            claimValue is true || (claimValue is string s && bool.TryParse(s, out var b) && b);

        private static string StateCacheKey(string state) => $"apple-oauth-state:{state}";

        public sealed record Payload(string Subject, string Email, bool EmailVerified, string? RefreshToken = null);
    }
}
