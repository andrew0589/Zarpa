using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data.Entities;
using NavigationES.Shared.Enums;

namespace NavigationES.Api.Data.Repositories
{
    public class ExamSessionRepository(NavigationESDbContext context) : IExamSessionRepository
    {
        private readonly NavigationESDbContext _context = context;

        public async Task<ExamEntity?> FindExamWithLicenseAsync(long examId)
        {
            return await _context.Exams
                .Include(e => e.License)
                .FirstOrDefaultAsync(e => e.ID == examId);
        }

        public async Task<List<ExamQuestionEntity>> GetExamQuestionsAsync(long examId)
        {
            return await _context.ExamQuestions
                .Include(q => q.Topic)
                .Where(q => q.ExamID == examId)
                .OrderBy(q => q.Position)
                .ToListAsync();
        }

        public async Task<Dictionary<long, int?>> GetTopicErrorLimitsAsync(long licenseId)
        {
            return await _context.LicenseTopics
                .Where(lt => lt.LicenseID == licenseId)
                .ToDictionaryAsync(lt => lt.TopicID, lt => lt.MaxErrors);
        }

        public async Task<TestSessionEntity?> FindOpenSessionAsync(long userId, long examId)
        {
            return await _context.TestSessions
                .Where(s => s.UserID == userId
                    && s.Mode == TestMode.ExamSimulation
                    && s.ExamID == examId
                    && s.FinishedAt == null)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<TestSessionEntity?> FindSessionAsync(long sessionId, long userId)
        {
            return await _context.TestSessions
                .FirstOrDefaultAsync(s => s.ID == sessionId && s.UserID == userId);
        }

        public async Task<ExamQuestionEntity?> FindExamQuestionAsync(long examQuestionId)
        {
            return await _context.ExamQuestions
                .FirstOrDefaultAsync(q => q.ID == examQuestionId);
        }

        public async Task<List<ExamSessionAnswerEntity>> GetAnswersAsync(long sessionId)
        {
            return await _context.ExamSessionAnswers
                .Where(a => a.SessionID == sessionId)
                .ToListAsync();
        }

        public async Task<ExamSessionAnswerEntity?> FindAnswerAsync(long sessionId, long examQuestionId)
        {
            return await _context.ExamSessionAnswers
                .FirstOrDefaultAsync(a => a.SessionID == sessionId && a.ExamQuestionID == examQuestionId);
        }

        public void AddSession(TestSessionEntity session)
        {
            _context.TestSessions.Add(session);
        }

        public async Task DeleteSessionsForExamAsync(long userId, long examId)
        {
            // ExamSessionAnswers cascade at the database level.
            await _context.TestSessions
                .Where(s => s.UserID == userId && s.ExamID == examId)
                .ExecuteDeleteAsync();
        }

        public async Task DeleteSessionAsync(TestSessionEntity session)
        {
            _context.TestSessions.Remove(session);
            await _context.SaveChangesAsync();
        }

        public void AddAnswer(ExamSessionAnswerEntity answer)
        {
            _context.ExamSessionAnswers.Add(answer);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
