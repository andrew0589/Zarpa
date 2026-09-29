using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using NavigationES.Shared.Constants;

namespace NavigationES.Api.Auth
{
    // Per-IP limits on the anonymous auth endpoints — the only ones reachable without
    // a token, so the only ones a script can hammer: guessing passwords or 6-digit
    // codes, or making the server mail strangers. The client IP is the one Traefik
    // reports (see the forwarded-headers setup in Program.cs), so a school or family
    // behind one NAT shares a budget; the numbers leave room for that.
    public static class AuthRateLimits
    {
        // signup, forgotPassword: every call sends an email.
        public const string SendsEmail = "auth-sends-email";
        // checkValidationCode: 6-digit codes, also capped per code (AuthService).
        public const string VerificationCode = "auth-verification-code";
        // signin, resetPassword: password guessing / reset-token probing.
        public const string Signin = "auth-signin";

        public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
        {
            return services.AddRateLimiter(options =>
            {
                options.AddPolicy(SendsEmail, http => PerIp(http, permits: 5, window: TimeSpan.FromMinutes(15)));
                options.AddPolicy(VerificationCode, http => PerIp(http, permits: 10, window: TimeSpan.FromMinutes(15)));
                options.AddPolicy(Signin, http => PerIp(http, permits: 10, window: TimeSpan.FromMinutes(1)));

                // Same body shape as the endpoints' own failures (both ResultDto's
                // errorMessage and ResultWithDataDto's errorCode carry the code), so a
                // client that reads the body gets a translatable code.
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.OnRejected = async (context, cancellationToken) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();

                    await context.HttpContext.Response.WriteAsJsonAsync(new
                    {
                        isSuccess = false,
                        errorCode = ErrorCodes.TooManyRequestsError,
                        errorMessage = ErrorCodes.TooManyRequestsError
                    }, cancellationToken);
                };
            });
        }

        private static RateLimitPartition<string> PerIp(HttpContext http, int permits, TimeSpan window) =>
            RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permits,
                    Window = window,
                    QueueLimit = 0
                });
    }
}
