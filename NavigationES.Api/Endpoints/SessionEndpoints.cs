using System.Security.Claims;
using NavigationES.Api.Auth;
using NavigationES.Api.Services;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Endpoints
{
    public static class SessionEndpoints
    {
        // Authenticated (fallback policy): sessions belong to the signed-in user.
        public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/sessions/topic/start",
                async (StartTopicPracticeRequestDto request, ClaimsPrincipal user, PracticeSessionService sessionService) =>
                    await sessionService.StartTopicPracticeAsync(user.GetUserId(), request) is { } session
                        ? Results.Ok(session)
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
