using System.Security.Claims;
using Zarpa.Api.Auth;
using Zarpa.Api.Services;
using Zarpa.Shared.Dtos;

namespace Zarpa.Api.Endpoints
{
    public static class ExamEndpoints
    {
        // Authenticated (fallback policy): the real-exam list and the simulation
        // sessions all belong to the signed-in user.
        public static IEndpointRouteBuilder MapExamEndpoints(this IEndpointRouteBuilder app)
        {
            // Filtered by the given license and by the account's community selection
            // (no selection = all communities).
            app.MapGet("/api/exams", async (long licenseId, ClaimsPrincipal user, ExamService examService) =>
                TypedResults.Ok(await examService.GetExamsAsync(user.GetUserId(), licenseId)));

            // Starts (or resumes) the timed simulation of a paper.
            app.MapPost("/api/exams/{examId:long}/sessions/start",
                async (long examId, ClaimsPrincipal user, ExamSessionService sessionService) =>
                    await sessionService.StartAsync(user.GetUserId(), examId) is { } session
                        ? Results.Ok(session)
                        : Results.BadRequest());

            // Records (or changes) the answer to one question.
            app.MapPost("/api/exams/sessions/{sessionId:long}/answers",
                async (long sessionId, SubmitExamAnswerRequestDto request, ClaimsPrincipal user, ExamSessionService sessionService) =>
                    await sessionService.SubmitAnswerAsync(user.GetUserId(), sessionId, request)
                        ? Results.Ok()
                        : Results.BadRequest());

            // Hands the paper in: grades it and returns the full report.
            app.MapPost("/api/exams/sessions/{sessionId:long}/finish",
                async (long sessionId, ClaimsPrincipal user, ExamSessionService sessionService) =>
                    await sessionService.FinishAsync(user.GetUserId(), sessionId) is { } result
                        ? Results.Ok(result)
                        : Results.BadRequest());

            // Re-reads the report of an already-graded attempt.
            app.MapGet("/api/exams/sessions/{sessionId:long}/result",
                async (long sessionId, ClaimsPrincipal user, ExamSessionService sessionService) =>
                    await sessionService.GetResultAsync(user.GetUserId(), sessionId) is { } result
                        ? Results.Ok(result)
                        : Results.BadRequest());

            return app;
        }
    }
}
