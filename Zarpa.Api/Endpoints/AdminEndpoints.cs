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

            return app;
        }
    }
}
