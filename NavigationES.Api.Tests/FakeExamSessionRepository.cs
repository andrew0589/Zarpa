using NavigationES.Api.Data.Entities;
using NavigationES.Api.Data.Repositories;

namespace NavigationES.Api.Tests
{
    // In-memory stand-in for the EF repository: the service under test runs against
    // plain lists, including the delete cascades the database would perform.
    public class FakeExamSessionRepository : IExamSessionRepository
    {
        public List<ExamEntity> Exams { get; } = [];
        public List<ExamQuestionEntity> Questions { get; } = [];
        public Dictionary<long, Dictionary<long, int?>> TopicLimitsByLicense { get; } = [];
        public List<TestSessionEntity> Sessions { get; } = [];
        public List<ExamSessionAnswerEntity> Answers { get; } = [];

        private long _nextSessionId = 1;
        private long _nextAnswerId = 1;

        public Task<ExamEntity?> FindExamWithLicenseAsync(long examId) =>
            Task.FromResult(Exams.FirstOrDefault(e => e.ID == examId));

        public Task<List<ExamQuestionEntity>> GetExamQuestionsAsync(long examId) =>
            Task.FromResult(Questions.Where(q => q.ExamID == examId).OrderBy(q => q.Position).ToList());

        public Task<Dictionary<long, int?>> GetTopicErrorLimitsAsync(long licenseId) =>
            Task.FromResult(TopicLimitsByLicense.GetValueOrDefault(licenseId) ?? []);

        public Task<TestSessionEntity?> FindOpenSessionAsync(long userId, long examId) =>
            Task.FromResult(Sessions
                .Where(s => s.UserID == userId && s.ExamID == examId && s.FinishedAt == null)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefault());

        public Task<TestSessionEntity?> FindSessionAsync(long sessionId, long userId) =>
            Task.FromResult(Sessions.FirstOrDefault(s => s.ID == sessionId && s.UserID == userId));

        public Task<ExamQuestionEntity?> FindExamQuestionAsync(long examQuestionId) =>
            Task.FromResult(Questions.FirstOrDefault(q => q.ID == examQuestionId));

        public Task<List<ExamSessionAnswerEntity>> GetAnswersAsync(long sessionId) =>
            Task.FromResult(Answers.Where(a => a.SessionID == sessionId).ToList());

        public Task<ExamSessionAnswerEntity?> FindAnswerAsync(long sessionId, long examQuestionId) =>
            Task.FromResult(Answers.FirstOrDefault(a => a.SessionID == sessionId && a.ExamQuestionID == examQuestionId));

        public void AddSession(TestSessionEntity session)
        {
            session.ID = _nextSessionId++;
            Sessions.Add(session);
        }

        public void AddAnswer(ExamSessionAnswerEntity answer)
        {
            answer.ID = _nextAnswerId++;
            answer.SessionID = answer.SessionID == 0 ? answer.Session?.ID ?? 0 : answer.SessionID;
            Answers.Add(answer);
        }

        public Task DeleteSessionsForExamAsync(long userId, long examId)
        {
            var doomed = Sessions.Where(s => s.UserID == userId && s.ExamID == examId).Select(s => s.ID).ToHashSet();
            Sessions.RemoveAll(s => doomed.Contains(s.ID));
            Answers.RemoveAll(a => doomed.Contains(a.SessionID));
            return Task.CompletedTask;
        }

        public Task DeleteSessionAsync(TestSessionEntity session)
        {
            Sessions.Remove(session);
            Answers.RemoveAll(a => a.SessionID == session.ID);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync() => Task.CompletedTask;
    }
}
