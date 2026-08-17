using System.Security.Claims;
using Zarpa.Api.Auth;
using Zarpa.Api.Services;
using Zarpa.Shared.Dtos;

namespace Zarpa.Api.Endpoints
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

            app.MapPost("/api/sessions/{sessionId:long}/answers",
                async (long sessionId, SubmitAnswerRequestDto request, ClaimsPrincipal user, PracticeSessionService sessionService) =>
                    await sessionService.SubmitAnswerAsync(user.GetUserId(), sessionId, request) is { } result
                        ? Results.Ok(result)
                        : Results.BadRequest());

            return app;
        }
    }
}
