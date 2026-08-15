using System.Security.Claims;

namespace Zarpa.Api.Auth
{
    public static class CurrentUserExtensions
    {
        // FallbackPolicy guarantees an authenticated principal on every non-anonymous
        // endpoint, and every token we issue carries nameid. 0 (never a valid ID) is
        // returned for malformed principals so downstream checks fail closed.
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);

            return long.TryParse(value, out var id) ? id : 0;
        }
    }
}
