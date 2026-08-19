using Refit;
using Zarpa.Shared.Dtos;

namespace Zarpa.ApiClient
{
    public interface IComunidadesApi
    {
        [Get("/api/comunidades")]
        Task<List<ComunidadDto>> GetComunidadesAsync();

        [Get("/api/comunidades/selected")]
        Task<SelectedComunidadDto> GetSelectedComunidadAsync();

        [Put("/api/comunidades/selected")]
        Task SelectComunidadAsync(SelectComunidadRequestDto request);
    }
}
