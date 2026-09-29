using System.Text.Json.Serialization;

namespace NavigationES.Api.Services
{
    // Server half of reCAPTCHA v3 on the website: asks Google whether the token the
    // page obtained is genuine, was issued for this action and scored as a human.
    //
    // Disabled (every check passes) while Recaptcha:SecretKey is unset or still the
    // appsettings placeholder, so a deploy without keys keeps signups working.
    public class RecaptchaService(HttpClient httpClient, IConfiguration configuration, ILogger<RecaptchaService> logger)
    {
        private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";
        private const string PlaceholderKey = "TODO";
        private const double DefaultMinScore = 0.5;

        private readonly HttpClient _httpClient = httpClient;
        private readonly ILogger<RecaptchaService> _logger = logger;
        private readonly string? _secretKey = configuration["Recaptcha:SecretKey"];
        private readonly double _minScore = configuration.GetValue("Recaptcha:MinScore", DefaultMinScore);

        public bool IsEnabled => !string.IsNullOrWhiteSpace(_secretKey) && _secretKey != PlaceholderKey;

        public async Task<bool> VerifyAsync(string? token, string expectedAction, string? remoteIp)
        {
            if (!IsEnabled) return true;
            if (string.IsNullOrWhiteSpace(token)) return false;

            var form = new Dictionary<string, string>
            {
                ["secret"] = _secretKey!,
                ["response"] = token
            };
            if (!string.IsNullOrEmpty(remoteIp)) form["remoteip"] = remoteIp;

            try
            {
                using var response = await _httpClient.PostAsync(VerifyUrl, new FormUrlEncodedContent(form));
                var result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>();

                if (result is null || !result.Success)
                {
                    _logger.LogWarning("reCAPTCHA rejected ({Action}): {Errors}", expectedAction,
                        result?.ErrorCodes is null ? "no body" : string.Join(",", result.ErrorCodes));
                    return false;
                }

                if (result.Action != expectedAction || result.Score < _minScore)
                {
                    _logger.LogWarning("reCAPTCHA failed ({Action}): action={ActualAction} score={Score}",
                        expectedAction, result.Action, result.Score);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Google unreachable: let the request through rather than lock every
                // web user out — the rate limits still apply.
                _logger.LogError(ex, "reCAPTCHA verification unavailable ({Action})", expectedAction);
                return true;
            }
        }

        private sealed record SiteVerifyResponse(
            [property: JsonPropertyName("success")] bool Success,
            [property: JsonPropertyName("score")] double Score,
            [property: JsonPropertyName("action")] string? Action,
            [property: JsonPropertyName("error-codes")] string[]? ErrorCodes);
    }
}
