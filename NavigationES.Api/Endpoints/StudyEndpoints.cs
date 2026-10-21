using System.Security.Claims;
using NavigationES.Api.Auth;
using NavigationES.Api.Services;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Endpoints
{
    public static class StudyEndpoints
    {
        // Authenticated (fallback policy). Everything except /access sits behind
        // StudyVisibilityEndpointFilter: administrators only until Study:Public is on.
        public static IEndpointRouteBuilder MapStudyEndpoints(this IEndpointRouteBuilder app)
        {
            // The clients ask this to decide whether to show the Estudiar tab. (The
            // ClaimsPrincipal parameter is not decoration: a lambda taking only
            // HttpContext binds as a RequestDelegate and its result is dropped.)
            app.MapGet("/api/study/access", async (HttpContext http, ClaimsPrincipal user) =>
                TypedResults.Ok(new StudyAccessDto(await StudyVisibilityEndpointFilter.IsOpenAsync(http))));

            var study = app.MapGroup("/api/study")
                .AddEndpointFilter<StudyVisibilityEndpointFilter>();

            study.MapGet("", async (long licenseId, ClaimsPrincipal user, StudyService service) =>
                TypedResults.Ok(await service.GetOverviewAsync(user.GetUserId(), licenseId)));

            study.MapGet("/topics/{number:int}", async (int number, ClaimsPrincipal user, StudyService service) =>
                await service.GetTopicAsync(user.GetUserId(), number) is { } topic ? Results.Ok(topic) : Results.NotFound());

            study.MapGet("/videos/{id:long}", async (long id, ClaimsPrincipal user, StudyService service) =>
                await service.GetVideoAsync(user.GetUserId(), id) is { } video ? Results.Ok(video) : Results.NotFound());

            // Called by the players every few seconds and on pause/end.
            study.MapPut("/videos/{id:long}/progress", async (long id, StudyProgressRequestDto request, ClaimsPrincipal user, StudyService service) =>
                await service.SaveProgressAsync(user.GetUserId(), id, request) ? Results.NoContent() : Results.NotFound());

            return app;
        }
    }
}
