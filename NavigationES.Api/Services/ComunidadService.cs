using NavigationES.Api.Data.Repositories;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    // Mirror of LicenseService for the autonomous communities: the list feeds the
    // pickers, the selection is stored per account so it follows the user across
    // devices, and it decides which communities' real exams the simulations offer.
    public class ComunidadService(IExamRepository examRepository, IUserRepository userRepository)
    {
        private readonly IExamRepository _examRepository = examRepository;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<List<ComunidadDto>> GetComunidadesAsync()
        {
            var comunidades = await _examRepository.GetAllComunidadesAsync();
            return [.. comunidades.Select(c => new ComunidadDto(c.ID, c.Name))];
        }

        public Task<long?> GetSelectedComunidadIdAsync(long userId) =>
            _userRepository.GetSelectedComunidadIdAsync(userId);

        public async Task<bool> SelectComunidadAsync(long userId, long comunidadId)
        {
            var comunidades = await _examRepository.GetAllComunidadesAsync();
            if (comunidades.All(c => c.ID != comunidadId))
                return false;

            return await _userRepository.SetSelectedComunidadIdAsync(userId, comunidadId);
        }
    }
}
