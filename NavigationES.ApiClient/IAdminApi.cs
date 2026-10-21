using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
{
    // The web's Usuarios tab. Administrators only — the API answers 403 to anyone
    // else, so the tab is hidden unless LoggedInUser.IsAdmin is set.
    public interface IAdminApi
    {
        // order: recent (default) | inactive | newest | name. Paged, 10 per page.
        [Get("/api/admin/users")]
        Task<AdminUserListDto> GetUsersAsync(string? search = null, string? order = null, int page = 1, int pageSize = 10, string? access = null);

        // Deletes all of the user's topic-practice and exam sessions; the account stays.
        [Post("/api/admin/users/{id}/clear-history")]
        Task<ResultDto> ClearHistoryAsync(long id);

        // Emails the Spanish "your account will be deleted in a month" notice.
        [Post("/api/admin/users/{id}/inactivity-warning")]
        Task<ResultDto> SendInactivityWarningAsync(long id);

        // Same removal as the user's own "Eliminar cuenta"; refused for administrators.
        [Delete("/api/admin/users/{id}")]
        Task<ResultDto> DeleteUserAsync(long id);

        // The Contenido tab: question bank per topic and license, exam simulations per comunidad.
        [Get("/api/admin/content")]
        Task<AdminContentDto> GetContentAsync();

        // The Convocatorias tab: per comunidad, the last imported sitting, the
        // convocatoria site and the next sitting expected.
        [Get("/api/admin/comunidades")]
        Task<List<AdminComunidadExamDto>> GetComunidadesAsync();

        // Saves the hand-kept fields (site, next date, done) of one comunidad.
        [Put("/api/admin/comunidades/{id}")]
        Task<ResultDto> UpdateComunidadAsync(long id, [Body] AdminComunidadUpdateDto request);

        // The Vídeos tab: every topic with all its videos, drafts included.
        [Get("/api/admin/study")]
        Task<List<AdminStudyTopicDto>> GetStudyAsync();

        // Chapter cover and blurb.
        [Put("/api/admin/study/topics/{number}")]
        Task<ResultDto> UpdateStudyTopicAsync(int number, [Body] AdminStudyTopicUpdateDto request);

        [Put("/api/admin/study/videos/{id}")]
        Task<ResultDto> UpdateStudyVideoAsync(long id, [Body] AdminStudyVideoUpdateDto request);

        [Delete("/api/admin/study/videos/{id}")]
        Task<ResultDto> DeleteStudyVideoAsync(long id);

        // The catalogue file written by NavigationES Documentation/Videos/exporta_catalogo.py.
        [Post("/api/admin/study/import")]
        Task<StudyImportResultDto> ImportStudyAsync([Body] List<StudyVideoImportDto> rows);

        // One .mp4 or .vtt into the API's videos folder; SuccessMessage = its relative URL.
        [Multipart]
        [Post("/api/admin/study/files")]
        Task<ResultDto> UploadStudyFileAsync([AliasAs("file")] StreamPart file);
    }
}
