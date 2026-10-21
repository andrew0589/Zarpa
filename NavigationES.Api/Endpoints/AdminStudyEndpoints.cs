using NavigationES.Api.Auth;
using NavigationES.Api.Services;
using Microsoft.AspNetCore.Mvc;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Endpoints
{
    // The web's Vídeos tab. Same guard as the other tabs: signed in (fallback JWT
    // policy) and flagged IsAdmin in the database (AdminUserEndpointFilter).
    public static class AdminStudyEndpoints
    {
        // The generated lessons are 5–20 MB; a long one stays far below this.
        private const long MaxUploadBytes = 500L * 1024 * 1024;

        public static IEndpointRouteBuilder MapAdminStudyEndpoints(this IEndpointRouteBuilder app)
        {
            var study = app.MapGroup("/api/admin/study")
                .AddEndpointFilter<AdminUserEndpointFilter>();

            study.MapGet("", async (AdminStudyService service) =>
                TypedResults.Ok(await service.GetAsync()));

            study.MapPut("/topics/{number:int}", async (int number, AdminStudyTopicUpdateDto request, AdminStudyService service) =>
                TypedResults.Ok(await service.UpdateTopicAsync(number, request)));

            study.MapPut("/videos/{id:long}", async (long id, AdminStudyVideoUpdateDto request, AdminStudyService service) =>
                TypedResults.Ok(await service.UpdateVideoAsync(id, request)));

            study.MapDelete("/videos/{id:long}", async (long id, AdminStudyService service) =>
                TypedResults.Ok(await service.DeleteVideoAsync(id)));

            // The catalogue file of Documentation/Videos/exporta_catalogo.py.
            study.MapPost("/import", async (List<StudyVideoImportDto> rows, AdminStudyService service) =>
                TypedResults.Ok(await service.ImportAsync(rows)));

            // One .mp4 or .vtt per request (multipart field "file"), saved under its own
            // name in the videos folder. The web sends a selection file by file, so the
            // request limit only has to fit the largest single video.
            study.MapPost("/files", async (HttpRequest request, AdminStudyService service) =>
                {
                    var form = await request.ReadFormAsync();
                    if (form.Files.GetFile("file") is not { } file)
                        return TypedResults.Ok(ResultDto.Failure(ErrorCodes.StudyFileNotValidError));

                    await using var stream = file.OpenReadStream();
                    return TypedResults.Ok(await service.SaveVideoFileAsync(file.FileName, stream));
                })
                // Bearer-token API: no cookies, so there is no cross-site form to forge.
                .DisableAntiforgery()
                .WithMetadata(new RequestSizeLimitAttribute(MaxUploadBytes))
                .WithFormOptions(multipartBodyLengthLimit: MaxUploadBytes);

            return app;
        }
    }
}
