using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;

namespace NavigationES.Api.Auth
{
    // Guards /api/admin/users/*: the caller must be signed in (the fallback JWT
    // policy) AND be flagged IsAdmin in the database. The flag is read on every call
    // instead of being baked into the JWT, so revoking admin takes effect immediately
    // rather than when the 7-day token expires. Non-admins get 403 — the web hides
    // the Usuarios tab from them anyway, so only a hand-crafted request lands here.
    public class AdminUserEndpointFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            var userId = http.User.GetUserId();
            if (userId <= 0)
                return Results.Forbid();

            var db = http.RequestServices.GetRequiredService<NavigationESDbContext>();
            var isAdmin = await db.Users.AsNoTracking()
                .Where(u => u.ID == userId)
                .Select(u => u.IsAdmin)
                .FirstOrDefaultAsync();

            return isAdmin ? await next(context) : Results.Forbid();
        }
    }
}
