using Zarpa.Api.Data.Entities;

namespace Zarpa.Api.Data.Repositories
{
    public interface IExamSessionRepository
    {
        // The paper with its license (timing + total error limit come from there).
        Task<ExamEntity?> FindExamWithLicenseAsync(long examId);

        // The paper's questions with their topics, in sheet order.
        Task<List<ExamQuestionEntity>> GetExamQuestionsAsync(long examId);

        // TopicID → per-topic error limit from the license blueprint (null = none).
        Task<Dictionary<long, int?>> GetTopicErrorLimitsAsync(long licenseId);

        // The user's unfinished simulation of this paper, if any — resumed on start.
        Task<TestSessionEntity?> FindOpenSessionAsync(long userId, long examId);

        // The session only when it belongs to the given user — sessions are private.
        Task<TestSessionEntity?> FindSessionAsync(long sessionId, long userId);

        Task<ExamQuestionEntity?> FindExamQuestionAsync(long examQuestionId);

        Task<List<ExamSessionAnswerEntity>> GetAnswersAsync(long sessionId);

        Task<ExamSessionAnswerEntity?> FindAnswerAsync(long sessionId, long examQuestionId);

        void AddSession(TestSessionEntity session);

        // Deletes ALL of the user's sessions for this paper (answers cascade).
        // Called when a fresh attempt starts: the exam list mirrors only the
        // latest attempt, older data is deliberately cleaned up.
        Task DeleteSessionsForExamAsync(long userId, long examId);

        // Deletes one abandoned session (answers cascade).
        Task DeleteSessionAsync(TestSessionEntity session);

        void AddAnswer(ExamSessionAnswerEntity answer);

        Task SaveChangesAsync();
    }
}
