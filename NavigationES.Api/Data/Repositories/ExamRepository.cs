using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data.Entities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Data.Repositories
{
    public class ExamRepository(NavigationESDbContext context) : IExamRepository
    {
        private readonly NavigationESDbContext _context = context;

        public async Task<ComunidadAutonomaEntity?> FindComunidadByNameAsync(string name)
        {
            var trimmed = name.Trim();
            return await _context.ComunidadesAutonomas
                .FirstOrDefaultAsync(c => c.Name == trimmed);
        }

        public async Task<List<ComunidadAutonomaEntity>> GetAllComunidadesAsync()
        {
            return await _context.ComunidadesAutonomas
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<List<ExamListItemDto>> GetExamsForListAsync(long userId, long licenseId, long? comunidadId)
        {
            return await _context.Exams
                .Where(e => e.LicenseID == licenseId
                    && (comunidadId == null || e.ComunidadAutonomaID == comunidadId))
                .OrderByDescending(e => e.Year)
                .ThenByDescending(e => e.Month)
                .ThenBy(e => e.Model)
                .Select(e => new ExamListItemDto(
                    e.ID,
                    e.Year,
                    e.Month,
                    e.Model,
                    e.ComunidadAutonoma.Name,
                    _context.ExamQuestions.Count(q => q.ExamID == e.ID),
                    _context.TestSessions.Any(s => s.UserID == userId && s.ExamID == e.ID),
                    _context.TestSessions.Any(s => s.UserID == userId && s.ExamID == e.ID && s.FinishedAt != null),
                    _context.TestSessions.Any(s => s.UserID == userId && s.ExamID == e.ID && s.Passed == true)))
                .ToListAsync();
        }

        public void AddExam(ExamEntity exam)
        {
            _context.Exams.Add(exam);
        }

        public void AddExamQuestion(ExamQuestionEntity link)
        {
            _context.ExamQuestions.Add(link);
        }

        public void DiscardChanges()
        {
            _context.ChangeTracker.Clear();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
