using Zarpa.Api.Data.Repositories;
using Zarpa.Shared.Dtos;

namespace Zarpa.Api.Services
{
    public class TopicService(ITopicRepository topicRepository)
    {
        private readonly ITopicRepository _topicRepository = topicRepository;

        public Task<TopicsProgressDto> GetTopicsWithProgressAsync(long userId, long licenseId) =>
            _topicRepository.GetTopicsWithProgressAsync(userId, licenseId);
    }
}
