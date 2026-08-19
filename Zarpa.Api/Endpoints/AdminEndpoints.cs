using Zarpa.Api.Services;
using Zarpa.Shared.Dtos;

namespace Zarpa.Api.Endpoints
{
    public static class AdminEndpoints
    {
        // Content-management surface. Mapped ONLY in Development (see Program.cs), so it
        // simply does not exist on a deployed API — which is why AllowAnonymous is safe:
        // it only ever answers on the developer's own machine, against the local DB.
        public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapPost("/api/admin/questions/import",
                async (List<QuestionImportDto> questions, QuestionImportService importService) =>
                    TypedResults.Ok(await importService.ImportAsync(questions)))
                .AllowAnonymous();

            // One full official exam paper (header + questions in sheet order).
            app.MapPost("/api/admin/exams/import",
                async (ExamImportDto exam, ExamImportService importService) =>
                    TypedResults.Ok(await importService.ImportAsync(exam)))
                .AllowAnonymous();

            // Bulk: an ARRAY of exam papers in one request. Each exam is imported
            // independently (its own all-or-nothing); one bad paper does not stop
            // the rest. Results come back in input order, labeled by sourceFile.
            app.MapPost("/api/admin/exams/import-bulk",
                async (List<ExamImportDto> exams, ExamImportService importService) =>
                {
                    var results = new List<object>();
                    foreach (var exam in exams)
                    {
                        var result = await importService.ImportAsync(exam);
                        results.Add(new { exam.SourceFile, Result = result });
                    }
                    return TypedResults.Ok(results);
                })
                .AllowAnonymous();

            return app;
        }
    }
}
