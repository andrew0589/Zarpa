using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
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
