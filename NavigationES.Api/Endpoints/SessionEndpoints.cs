using System.Security.Claims;
using NavigationES.Api.Auth;
using NavigationES.Api.Services;
using NavigationES.Shared.Dtos;
using NavigationES.Shared.Enums;

namespace NavigationES.Api.Endpoints
{
    public static class SessionEndpoints
    {
        // Authenticated (fallback policy): sessions belong to the signed-in user.
        // Starting (or resuming) practice needs the trial or a subscription
        // (SubscriptionEndpointFilter); answering an open session does not.
        public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/sessions/topic/start",
                async (StartTopicPracticeRequestDto request, ClaimsPrincipal user, PracticeSessionService sessionService) =>
                    await sessionService.StartTopicPracticeAsync(user.GetUserId(), request) is { } session
                        ? Results.Ok(session)
                        : Results.BadRequest())
                .AddEndpointFilter<SubscriptionEndpointFilter>();

            // "Repasar mis fallos" / "Mini test": license-wide sessions. 400 when there is
            // nothing to plan (no failed questions, empty license).
            app.MapPost("/api/sessions/review/start",
                async (StartLicensePracticeRequestDto request, ClaimsPrincipal user, PracticeSessionService sessionService) =>
                    await sessionService.StartLicensePracticeAsync(user.GetUserId(), request.LicenseId, TestMode.Review) is { } session
                        ? Results.Ok(session)
                        : Results.BadRequest())
                .AddEndpointFilter<SubscriptionEndpointFilter>();

            app.MapPost("/api/sessions/quick/start",
                async (StartLicensePracticeRequestDto request, ClaimsPrincipal user, PracticeSessionService sessionService) =>
                    await sessionService.StartLicensePracticeAsync(user.GetUserId(), request.LicenseId, TestMode.Quick) is { } session
                        ? Results.Ok(session)
                        : Results.BadRequest())
                .AddEndpointFilter<SubscriptionEndpointFilter>();

            // Inicio: where the user left off, failed-question count, next official sitting.
            app.MapGet("/api/dashboard",
                async (long licenseId, ClaimsPrincipal user, DashboardService dashboard) =>
                    await dashboard.GetAsync(user.GetUserId(), licenseId) is { } dto
                        ? Results.Ok(dto)
                        : Results.BadRequest());

            app.MapPost("/api/sessions/topic/reset",
                async (ResetTopicPracticeRequestDto request, ClaimsPrincipal user, PracticeSessionService sessionService) =>
                    await sessionService.ResetTopicPracticeAsync(user.GetUserId(), request)
                        ? Results.Ok()
                        : Results.BadRequest());

            app.MapPost("/api/sessions/{sessionId:long}/answers",
                async (long sessionId, SubmitAnswerRequestDto request, ClaimsPrincipal user, PracticeSessionService sessionService) =>
                    await sessionService.SubmitAnswerAsync(user.GetUserId(), sessionId, request) is { } result
                        ? Results.Ok(result)
                        : Results.BadRequest());

            return app;
        }
    }
}
