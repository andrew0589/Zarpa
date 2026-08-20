using Zarpa.Api.Auth;
using Zarpa.Api.Services;
using Zarpa.Shared.Dtos;

namespace Zarpa.Api.Endpoints
{
    public static class AdminEndpoints
    {
        // Content-management surface. Reachable in every environment but gated by the
        // AdminKeyEndpointFilter (X-Admin-Key header); AllowAnonymous defers auth to
        // that filter instead of the JWT fallback policy. The filter fails closed, so
        // a deploy without a configured key exposes nothing.
        public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
        {
            var admin = app.MapGroup("/api/admin")
                .AllowAnonymous()
                .AddEndpointFilter<AdminKeyEndpointFilter>();

            admin.MapPost("/questions/import",
                async (List<QuestionImportDto> questions, QuestionImportService importService) =>
                    TypedResults.Ok(await importService.ImportAsync(questions)));

            // One full official exam paper (header + questions in sheet order).
            admin.MapPost("/exams/import",
                async (ExamImportDto exam, ExamImportService importService) =>
                    TypedResults.Ok(await importService.ImportAsync(exam)));

            // Bulk: an ARRAY of exam papers in one request. Each exam is imported
            // independently (its own all-or-nothing); one bad paper does not stop
            // the rest. Results come back in input order, labeled by sourceFile.
            admin.MapPost("/exams/import-bulk",
                async (List<ExamImportDto> exams, ExamImportService importService) =>
                {
                    var results = new List<object>();
                    foreach (var exam in exams)
                    {
                        var result = await importService.ImportAsync(exam);
                        results.Add(new { exam.SourceFile, Result = result });
                    }
                    return TypedResults.Ok(results);
                });

            return app;
        }
    }
}
