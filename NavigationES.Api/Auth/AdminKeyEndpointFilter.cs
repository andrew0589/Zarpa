using System.Security.Cryptography;
using System.Text;

namespace NavigationES.Api.Auth
{
    // Guards the /api/admin/* content-management endpoints with a shared secret in the
    // "X-Admin-Key" header. These endpoints are AllowAnonymous (the key IS the auth),
    // so without this filter they would be wide open.
    //
    // Fails closed: if no key is configured (or it is still the appsettings
    // placeholder), the endpoints answer 404 as if they did not exist — a deploy that
    // forgot to set the secret cannot expose an unauthenticated import surface.
    public class AdminKeyEndpointFilter : IEndpointFilter
    {
        private const string HeaderName = "X-Admin-Key";
        private const string PlaceholderKey = "TODO";

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            var configuredKey = http.RequestServices
                .GetRequiredService<IConfiguration>()["AdminSettings:ApiKey"];

            if (string.IsNullOrWhiteSpace(configuredKey) || configuredKey == PlaceholderKey)
                return Results.NotFound();

            var providedKey = http.Request.Headers[HeaderName].ToString();
            if (!FixedTimeEquals(providedKey, configuredKey))
                return Results.Unauthorized();

            return await next(context);
        }

        // Constant-time comparison so a wrong key cannot be recovered byte-by-byte
        // from response timing.
        private static bool FixedTimeEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
