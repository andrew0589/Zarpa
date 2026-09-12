using NavigationES.Api.Auth;
using NavigationES.Api.Services;

namespace NavigationES.Api.Endpoints
{
    // User management for the web's Usuarios tab. Unlike /api/admin (X-Admin-Key, for
    // import scripts), these are called by a signed-in person: the JWT fallback
    // policy applies, plus the IsAdmin database check in AdminUserEndpointFilter.
    public static class AdminUserEndpoints
    {
        public static IEndpointRouteBuilder MapAdminUserEndpoints(this IEndpointRouteBuilder app)
        {
            var users = app.MapGroup("/api/admin/users")
                .AddEndpointFilter<AdminUserEndpointFilter>();

            // ?search= matches name or email; ?order= recent (default) | inactive |
            // newest | name; ?page= (10 per page; a ?pageSize= above 10 is clamped).
            users.MapGet("", async (string? search, string? order, int? page, int? pageSize, AdminUserService service) =>
                TypedResults.Ok(await service.GetUsersAsync(search, order, page ?? 1, pageSize ?? AdminUserService.DefaultPageSize)));

            users.MapPost("/{id:long}/clear-history", async (long id, AdminUserService service) =>
                TypedResults.Ok(await service.ClearHistoryAsync(id)));

            users.MapPost("/{id:long}/inactivity-warning", async (long id, AdminUserService service) =>
                TypedResults.Ok(await service.SendInactivityWarningAsync(id)));

            users.MapDelete("/{id:long}", async (long id, AdminUserService service) =>
                TypedResults.Ok(await service.DeleteUserAsync(id)));

            return app;
        }
    }
}
