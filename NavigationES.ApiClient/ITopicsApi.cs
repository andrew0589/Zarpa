using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
{
    public interface ITopicsApi
    {
        [Get("/api/topics")]
        Task<TopicsProgressDto> GetTopicsAsync(long licenseId);
    }
}
