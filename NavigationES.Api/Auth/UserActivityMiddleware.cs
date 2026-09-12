using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NavigationES.Api.Data;

namespace NavigationES.Api.Auth
{
    // Records when — and from which client — each user last called the API: the
    // "last used" tracker behind the admin Usuarios tab. Runs after authentication,
    // so ANY request carrying a valid JWT counts as activity: a sign-in, but also a
    // question answered a week later on a token restored from storage (tokens last
    // 7 days, so tracking sign-ins alone would miss most real usage).
    //
    // Cost control: a user fires many calls per minute, so the row is written at most
    // once per MinInterval per client (guarded by IMemoryCache, per API instance) and
    // with a single UPDATE that never loads the entity. A failed write is logged and
    // swallowed — tracking must never break the request it rides on.
    //
    // Side benefit: the UPDATE touching zero rows means the token belongs to an account
    // that no longer exists (deleted by an administrator, or from Perfil on another
    // device). Such requests get 401, which every client already turns into a sign-out,
    // instead of limping along on a valid signature for up to 7 days.
    public class UserActivityMiddleware(RequestDelegate next, IMemoryCache cache, ILogger<UserActivityMiddleware> logger)
    {
        // Sent by every client: "web" (NavigationES.Web), "android"/"ios"/"windows"
        // (the MAUI app). Anything else — or nothing, from app builds older than the
        // header — is filed under "app".
        public const string ClientHeader = "X-Client";
        public const string UnknownClient = "app";
        public static readonly TimeSpan MinInterval = TimeSpan.FromMinutes(30);

        private static readonly string[] KnownClients = ["web", "android", "ios", "windows", UnknownClient];

        private readonly RequestDelegate _next = next;
        private readonly IMemoryCache _cache = cache;
        private readonly ILogger<UserActivityMiddleware> _logger = logger;

        public async Task InvokeAsync(HttpContext http, NavigationESDbContext db)
        {
            // Anonymous endpoints (sign-in, social callbacks, legal pages) are left
            // alone even when a stale token rides along — a deleted user must still be
            // able to sign up or sign in again.
            var allowsAnonymous = http.GetEndpoint()?.Metadata.GetMetadata<IAllowAnonymous>() is not null;

            if (!allowsAnonymous && http.User.Identity?.IsAuthenticated == true)
            {
                var userId = http.User.GetUserId();
                if (userId > 0 && !await RecordAsync(db, userId, NormalizeClient(http.Request.Headers[ClientHeader])))
                {
                    http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
            }

            await _next(http);
        }

        public static string NormalizeClient(string? header)
        {
            var value = header?.Trim().ToLowerInvariant();
            return !string.IsNullOrEmpty(value) && KnownClients.Contains(value) ? value : UnknownClient;
        }

        // Called when an account is deleted so the very next request on its token is
        // rejected instead of riding the throttle window for up to MinInterval.
        public static void Forget(IMemoryCache cache, long userId)
        {
            foreach (var client in KnownClients)
                cache.Remove(CacheKey(userId, client));
        }

        // False only when the account is known NOT to exist; any failure to record
        // counts as "exists" so tracking can never lock a real user out.
        private async Task<bool> RecordAsync(NavigationESDbContext db, long userId, string client)
        {
            // Keyed per client too, so switching from the app to the web within the
            // interval is still recorded (the row shows where the user was last).
            var cacheKey = CacheKey(userId, client);
            if (_cache.TryGetValue(cacheKey, out _))
                return true;

            try
            {
                var now = DateTime.UtcNow;
                var affected = await db.Users
                    .Where(u => u.ID == userId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(u => u.LastActiveAt, now)
                        .SetProperty(u => u.LastActiveClient, client));

                if (affected == 0)
                    return false;

                _cache.Set(cacheKey, true, MinInterval);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not record activity for user {UserId}", userId);
                return true;
            }
        }

        private static string CacheKey(long userId, string client) => $"user-activity:{userId}:{client}";
    }
}
