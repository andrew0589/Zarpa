using System.Security.Claims;
using NavigationES.Api.Auth;
using NavigationES.Api.Services;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Endpoints
{
    public static class ComunidadEndpoints
    {
        // Authenticated (fallback policy): the list feeds the pickers (Perfil and
        // first sign-in), the selection is stored per account.
        public static IEndpointRouteBuilder MapComunidadEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/comunidades", async (ComunidadService comunidadService) =>
                TypedResults.Ok(await comunidadService.GetComunidadesAsync()));

            app.MapGet("/api/comunidades/selected", async (ClaimsPrincipal user, ComunidadService comunidadService) =>
                TypedResults.Ok(new SelectedComunidadDto(await comunidadService.GetSelectedComunidadIdAsync(user.GetUserId()))));

            app.MapPut("/api/comunidades/selected", async (SelectComunidadRequestDto request, ClaimsPrincipal user, ComunidadService comunidadService) =>
                await comunidadService.SelectComunidadAsync(user.GetUserId(), request.ComunidadId)
                    ? Results.Ok()
                    : Results.BadRequest());

            return app;
        }
    }
}
