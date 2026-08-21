using NavigationES.Api.Data.Repositories;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    // Read side of the real exams: the picker list for the simulation screen.
    // The user's stored selections drive the filters: license is mandatory,
    // community narrows the list only when the user has chosen one.
    public class ExamService(IExamRepository examRepository, IUserRepository userRepository)
    {
        private readonly IExamRepository _examRepository = examRepository;
        private readonly IUserRepository _userRepository = userRepository;

        public async Task<List<ExamListItemDto>?> GetExamsAsync(long userId, long licenseId)
        {
            var comunidadId = await _userRepository.GetSelectedComunidadIdAsync(userId);
            return await _examRepository.GetExamsForListAsync(userId, licenseId, comunidadId);
        }
    }
}
