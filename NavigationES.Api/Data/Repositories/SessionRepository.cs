using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data.Entities;
using NavigationES.Shared.Enums;

namespace NavigationES.Api.Data.Repositories
{
    public class SessionRepository(NavigationESDbContext context) : ISessionRepository
    {
        private readonly NavigationESDbContext _context = context;

        public async Task<TestSessionEntity?> FindOpenTopicSessionAsync(long userId, long topicId, long licenseId)
        {
            return await _context.TestSessions
                .Where(s => s.UserID == userId
                    && s.Mode == TestMode.TopicPractice
                    && s.TopicID == topicId
                    && s.LicenseID == licenseId
                    && s.FinishedAt == null)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<TestSessionEntity?> FindByIdAsync(long sessionId, long userId)
        {
            return await _context.TestSessions
                .FirstOrDefaultAsync(s => s.ID == sessionId && s.UserID == userId);
        }

        public async Task<HashSet<long>> GetAnsweredQuestionIdsAsync(long sessionId)
        {
            var ids = await _context.SessionAnswers
                .Where(sa => sa.SessionID == sessionId)
                .Select(sa => sa.QuestionID)
                .ToListAsync();

            return [.. ids];
        }

        public async Task<HashSet<long>> GetPlannedQuestionIdsAsync(long sessionId)
        {
            var ids = await _context.SessionQuestions
                .Where(sq => sq.SessionID == sessionId)
                .Select(sq => sq.QuestionID)
                .ToListAsync();

            return [.. ids];
        }

        public async Task<List<long>> GetLatestFailedQuestionIdsAsync(long userId, long topicId)
        {
            // Answer IDs are monotonically increasing, so the max ID per question is
            // its most recent answer.
            var latestAnswerIds = _context.SessionAnswers
                .Where(sa => sa.Session.UserID == userId
                    && sa.Question.TopicID == topicId
                    && sa.Question.IsActive)
                .GroupBy(sa => sa.QuestionID)
                .Select(g => g.Max(sa => sa.ID));

            return await _context.SessionAnswers
                .Where(sa => latestAnswerIds.Contains(sa.ID) && !sa.IsCorrect)
                .Select(sa => sa.QuestionID)
                .ToListAsync();
        }

        public void AddPlannedQuestions(TestSessionEntity session, IEnumerable<long> questionIds)
        {
            foreach (var questionId in questionIds)
                _context.SessionQuestions.Add(new SessionQuestionEntity { Session = session, QuestionID = questionId });
        }

        public async Task<SessionAnswerEntity?> FindAnswerAsync(long sessionId, long questionId)
        {
            return await _context.SessionAnswers
                .FirstOrDefaultAsync(sa => sa.SessionID == sessionId && sa.QuestionID == questionId);
        }

        public async Task<int> CountAnswersAsync(long sessionId)
        {
            return await _context.SessionAnswers.CountAsync(sa => sa.SessionID == sessionId);
        }

        public async Task DeleteTopicSessionsAsync(long userId, long topicId)
        {
            // SessionQuestions and SessionAnswers cascade from their session, and the
            // planning query is license-agnostic, so all licenses' sessions must go.
            await _context.TestSessions
                .Where(s => s.UserID == userId
                    && s.Mode == TestMode.TopicPractice
                    && s.TopicID == topicId)
                .ExecuteDeleteAsync();
        }

        public void AddSession(TestSessionEntity session)
        {
            _context.TestSessions.Add(session);
        }

        public void AddAnswer(SessionAnswerEntity answer)
        {
            _context.SessionAnswers.Add(answer);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
