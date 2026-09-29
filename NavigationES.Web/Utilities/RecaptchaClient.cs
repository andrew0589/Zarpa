using Microsoft.JSInterop;

namespace NavigationES.Web.Utilities
{
    // Blazor side of wwwroot/js/recaptcha.js. The site key comes from appsettings.json
    // (RecaptchaSiteKey, written at container start); without one every token is null
    // and the forms work as before.
    public class RecaptchaClient(IJSRuntime js, string? siteKey)
    {
        private readonly IJSRuntime _js = js;
        private readonly string? _siteKey = string.IsNullOrWhiteSpace(siteKey) ? null : siteKey;

        public async Task PreloadAsync()
        {
            if (_siteKey is null) return;
            await _js.InvokeVoidAsync("navigationesRecaptcha.preload", _siteKey);
        }

        // The action must match the one the API checks (RecaptchaEndpointFilter).
        public async Task<string?> GetTokenAsync(string action)
        {
            if (_siteKey is null) return null;
            return await _js.InvokeAsync<string?>("navigationesRecaptcha.execute", _siteKey, action);
        }
    }
}
