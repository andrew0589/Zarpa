using Refit;
using Zarpa.Shared.Dtos;

namespace Zarpa.Client.Services
{
    public interface ITopicsApi
    {
        [Get("/api/topics")]
        Task<TopicsProgressDto> GetTopicsAsync(long licenseId);
    }
}
