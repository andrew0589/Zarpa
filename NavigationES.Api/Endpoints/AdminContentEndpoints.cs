using NavigationES.Api.Auth;
using NavigationES.Api.Services;

namespace NavigationES.Api.Endpoints
{
    // Catalogue statistics for the web's Contenido tab. Same guard as the Usuarios
    // tab: the caller must be signed in (fallback JWT policy) and flagged IsAdmin in
    // the database (AdminUserEndpointFilter). Read-only.
    public static class AdminContentEndpoints
    {
        public static IEndpointRouteBuilder MapAdminContentEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGroup("/api/admin/content")
                .AddEndpointFilter<AdminUserEndpointFilter>()
                .MapGet("", async (AdminContentService service) => TypedResults.Ok(await service.GetAsync()));

            return app;
        }
    }
}
