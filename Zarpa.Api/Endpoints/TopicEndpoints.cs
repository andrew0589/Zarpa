using System.Security.Claims;
using Zarpa.Api.Auth;
using Zarpa.Api.Services;

namespace Zarpa.Api.Endpoints
{
    public static class TopicEndpoints
    {
        // Authenticated (fallback policy): progress is per user, derived live from the
        // session-answer history — no separate progress table to keep in sync.
        public static IEndpointRouteBuilder MapTopicEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/api/topics", async (long licenseId, ClaimsPrincipal user, TopicService topicService) =>
                TypedResults.Ok(await topicService.GetTopicsWithProgressAsync(user.GetUserId(), licenseId)));

            return app;
        }
    }
}
