using Zarpa.Api.Data.Entities;

namespace Zarpa.Api.Data.Repositories
{
    public interface ISessionRepository
    {
        // The user's unfinished TopicPractice session for this topic and license, if any.
        Task<TestSessionEntity?> FindOpenTopicSessionAsync(long userId, long topicId, long licenseId);

        // The session only when it belongs to the given user — sessions are private.
        Task<TestSessionEntity?> FindByIdAsync(long sessionId, long userId);

        Task<HashSet<long>> GetAnsweredQuestionIdsAsync(long sessionId);

        // The question set the session was created to cover.
        Task<HashSet<long>> GetPlannedQuestionIdsAsync(long sessionId);

        // Question IDs of the topic whose LATEST answer by this user was wrong —
        // the retry set a new session starts with.
        Task<List<long>> GetLatestFailedQuestionIdsAsync(long userId, long topicId);

        void AddPlannedQuestions(TestSessionEntity session, IEnumerable<long> questionIds);

        Task<SessionAnswerEntity?> FindAnswerAsync(long sessionId, long questionId);

        Task<int> CountAnswersAsync(long sessionId);

        // Wipes the user's TopicPractice history for the topic (planned questions and
        // answers go with their sessions). Executes immediately, outside SaveChanges.
        Task DeleteTopicSessionsAsync(long userId, long topicId);

        void AddSession(TestSessionEntity session);

        void AddAnswer(SessionAnswerEntity answer);

        Task SaveChangesAsync();
    }
}
