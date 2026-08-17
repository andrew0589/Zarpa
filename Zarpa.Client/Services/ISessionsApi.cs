using Refit;
using Zarpa.Shared.Dtos;

namespace Zarpa.Client.Services
{
    public interface ISessionsApi
    {
        [Post("/api/sessions/topic/start")]
        Task<PracticeSessionDto> StartTopicPracticeAsync(StartTopicPracticeRequestDto request);

        [Post("/api/sessions/{sessionId}/answers")]
        Task<SubmitAnswerResultDto> SubmitAnswerAsync(long sessionId, SubmitAnswerRequestDto request);
    }
}
