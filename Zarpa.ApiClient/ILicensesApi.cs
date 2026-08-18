using Refit;
using Zarpa.Shared.Dtos;

namespace Zarpa.ApiClient
{
    public interface ILicensesApi
    {
        [Get("/api/licenses")]
        Task<List<LicenseDto>> GetLicensesAsync();

        [Get("/api/licenses/selected")]
        Task<SelectedLicenseDto> GetSelectedLicenseAsync();

        [Put("/api/licenses/selected")]
        Task SelectLicenseAsync(SelectLicenseRequestDto request);
    }
}
