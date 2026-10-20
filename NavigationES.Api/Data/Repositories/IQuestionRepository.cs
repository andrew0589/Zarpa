using NavigationES.Api.Data.Entities;

namespace NavigationES.Api.Data.Repositories
{
    public interface IQuestionRepository
    {
        // The question with the given normalized-statement hash, answers included;
        // null when the statement is not in the bank yet.
        Task<QuestionEntity?> FindByContentHashAsync(string contentHash);

        // Active questions of a topic in a stable order (by ID), answers included —
        // the stable order is what makes resuming a session deterministic.
        Task<List<QuestionEntity>> GetActiveByTopicAsync(long topicId);

        Task<QuestionEntity?> FindActiveWithAnswersAsync(long questionId);

        // Active questions with the given IDs, answers included, ordered by ID.
        Task<List<QuestionEntity>> GetActiveByIdsAsync(IReadOnlyCollection<long> questionIds);

        // Up to `count` random active question IDs from the given topics.
        Task<List<long>> GetRandomActiveIdsAsync(IReadOnlyCollection<long> topicIds, int count);

        void Add(QuestionEntity question);

        Task SaveChangesAsync();
    }
}
