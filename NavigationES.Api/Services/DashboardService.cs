using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;
using NavigationES.Api.Data.Repositories;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    // Behind the web's Inicio page: the few things it shows that topic and exam
    // progress do not already carry — where the user left off, how many questions
    // are waiting in "Repasar mis fallos", and the next official sitting.
    public class DashboardService(NavigationESDbContext context, ISessionRepository sessionRepository, ILicenseRepository licenseRepository)
    {
        private readonly NavigationESDbContext _context = context;
        private readonly ISessionRepository _sessionRepository = sessionRepository;
        private readonly ILicenseRepository _licenseRepository = licenseRepository;

        public async Task<DashboardDto?> GetAsync(long userId, long licenseId)
        {
            var topicIds = await _licenseRepository.GetTopicIdsAsync(licenseId);
            if (topicIds.Count == 0)
                return null;

            DashboardTopicDto? lastTopic = null;
            var session = await _sessionRepository.FindLatestTopicSessionAsync(userId, licenseId);
            if (session?.Topic is not null && topicIds.Contains(session.Topic.ID))
            {
                var planned = await _sessionRepository.GetPlannedQuestionIdsAsync(session.ID);
                var answered = await _sessionRepository.GetAnsweredQuestionIdsAsync(session.ID);
                var finished = session.FinishedAt is not null || planned.All(answered.Contains);
                lastTopic = new DashboardTopicDto(
                    session.Topic.Number, session.Topic.Name,
                    finished ? planned.Count : answered.Count, planned.Count, finished);
            }

            var failed = await _sessionRepository.GetLatestFailedQuestionIdsAsync(userId, topicIds);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var nextExam = await _context.Users
                .Where(u => u.ID == userId && u.SelectedComunidadAutonomaID != null)
                .Join(_context.ComunidadesAutonomas, u => u.SelectedComunidadAutonomaID, c => c.ID, (u, c) => c)
                .Where(c => c.NextExamDate != null && c.NextExamDate >= today)
                .Select(c => new DashboardExamDateDto(c.Name, c.NextExamDate!.Value))
                .FirstOrDefaultAsync();

            var myExamDate = await _context.Users.Where(u => u.ID == userId).Select(u => u.ExamDate).FirstOrDefaultAsync();

            return new DashboardDto(lastTopic, failed.Count, nextExam, PracticeSessionService.QuickTestSize, myExamDate);
        }
    }
}
