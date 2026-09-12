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
        Task<AdminUserListDto> GetUsersAsync(string? search = null, string? order = null, int page = 1, int pageSize = 10);

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
    }
}
