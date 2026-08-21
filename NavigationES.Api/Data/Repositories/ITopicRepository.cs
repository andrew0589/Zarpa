using NavigationES.Api.Data.Entities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Data.Repositories
{
    public interface ITopicRepository
    {
        // The topics of the given license's exam blueprint, each with its active-question
        // pool size and how many of those questions the user has answered correctly —
        // plus global totals computed over the entire bank, all topics included.
        Task<TopicsProgressDto> GetTopicsWithProgressAsync(long userId, long licenseId);

        // The topics keyed by their official number (1–11), for import validation.
        Task<Dictionary<int, TopicEntity>> GetByNumberAsync();
    }
}
