using Refit;
using Zarpa.Shared.Dtos;

namespace Zarpa.ApiClient
{
    public interface ISessionsApi
    {
        [Post("/api/sessions/topic/start")]
        Task<PracticeSessionDto> StartTopicPracticeAsync(StartTopicPracticeRequestDto request);

        [Post("/api/sessions/topic/reset")]
        Task ResetTopicPracticeAsync(ResetTopicPracticeRequestDto request);

        [Post("/api/sessions/{sessionId}/answers")]
        Task<SubmitAnswerResultDto> SubmitAnswerAsync(long sessionId, SubmitAnswerRequestDto request);
    }
}
