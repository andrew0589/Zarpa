using NavigationES.Api.Data.Repositories;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    public class TopicService(ITopicRepository topicRepository)
    {
        private readonly ITopicRepository _topicRepository = topicRepository;

        public Task<TopicsProgressDto> GetTopicsWithProgressAsync(long userId, long licenseId) =>
            _topicRepository.GetTopicsWithProgressAsync(userId, licenseId);
    }
}
