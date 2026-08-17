using System.Security.Claims;
using Zarpa.Api.Auth;
using Zarpa.Api.Services;
using Zarpa.Shared.Dtos;

namespace Zarpa.Api.Endpoints
{
    public static class LicenseEndpoints
    {
        // Authenticated (fallback policy): the license list feeds the picker on the
        // Tests tab, and the selection is stored per account so it follows the user
        // across devices and reinstalls.
        public static IEndpointRouteBuilder MapLicenseEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/licenses", async (LicenseService licenseService) =>
                TypedResults.Ok(await licenseService.GetLicensesAsync()));

            app.MapGet("/api/licenses/selected", async (ClaimsPrincipal user, LicenseService licenseService) =>
                TypedResults.Ok(new SelectedLicenseDto(await licenseService.GetSelectedLicenseIdAsync(user.GetUserId()))));

            app.MapPut("/api/licenses/selected", async (SelectLicenseRequestDto request, ClaimsPrincipal user, LicenseService licenseService) =>
                await licenseService.SelectLicenseAsync(user.GetUserId(), request.LicenseId)
                    ? Results.Ok()
                    : Results.BadRequest());

            return app;
        }
    }
}
