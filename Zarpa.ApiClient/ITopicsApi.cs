using Refit;
using Zarpa.Shared.Dtos;

namespace Zarpa.ApiClient
{
    public interface ITopicsApi
    {
        [Get("/api/topics")]
        Task<TopicsProgressDto> GetTopicsAsync(long licenseId);
    }
}
