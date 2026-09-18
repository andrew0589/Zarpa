using NavigationES.Api.Auth;
using NavigationES.Api.Services;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Endpoints
{
    // The web's Convocatorias tab: last imported sitting, convocatoria site and next
    // expected sitting per comunidad autónoma. Same guard as the other tabs: the
    // caller must be signed in (fallback JWT policy) and flagged IsAdmin in the
    // database (AdminUserEndpointFilter).
    public static class AdminComunidadEndpoints
    {
        public static IEndpointRouteBuilder MapAdminComunidadEndpoints(this IEndpointRouteBuilder app)
        {
            var comunidades = app.MapGroup("/api/admin/comunidades")
                .AddEndpointFilter<AdminUserEndpointFilter>();

            comunidades.MapGet("", async (AdminComunidadService service) =>
                TypedResults.Ok(await service.GetAsync()));

            // Stores the three hand-kept fields (site, next date, done) exactly as sent.
            comunidades.MapPut("/{id:long}", async (long id, AdminComunidadUpdateDto request, AdminComunidadService service) =>
                TypedResults.Ok(await service.UpdateAsync(id, request)));

            return app;
        }
    }
}
