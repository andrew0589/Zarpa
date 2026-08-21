using NavigationES.Api.Data.Repositories;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    public class LicenseService(ILicenseRepository licenseRepository, IUserRepository userRepository)
    {
        private readonly ILicenseRepository _licenseRepository = licenseRepository;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<List<LicenseDto>> GetLicensesAsync()
        {
            var licenses = await _licenseRepository.GetAllAsync();
            return [.. licenses.Select(l => new LicenseDto(l.ID, l.Code, l.Name))];
        }

        public Task<long?> GetSelectedLicenseIdAsync(long userId) =>
            _userRepository.GetSelectedLicenseIdAsync(userId);

        public async Task<bool> SelectLicenseAsync(long userId, long licenseId)
        {
            var licenses = await _licenseRepository.GetAllAsync();
            if (licenses.All(l => l.ID != licenseId))
                return false;

            return await _userRepository.SetSelectedLicenseIdAsync(userId, licenseId);
        }
    }
}
