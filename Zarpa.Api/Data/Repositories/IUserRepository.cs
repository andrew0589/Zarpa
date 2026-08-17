namespace Zarpa.Api.Data.Repositories
{
    public interface IUserRepository
    {
        Task<long?> GetSelectedLicenseIdAsync(long userId);

        Task<bool> SetSelectedLicenseIdAsync(long userId, long licenseId);
    }
}
