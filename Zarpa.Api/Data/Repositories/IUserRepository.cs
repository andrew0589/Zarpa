namespace Zarpa.Api.Data.Repositories
{
    public interface IUserRepository
    {
        Task<long?> GetSelectedLicenseIdAsync(long userId);

        Task<bool> SetSelectedLicenseIdAsync(long userId, long licenseId);

        Task<long?> GetSelectedComunidadIdAsync(long userId);

        Task<bool> SetSelectedComunidadIdAsync(long userId, long comunidadId);
    }
}
