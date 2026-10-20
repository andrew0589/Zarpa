using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
{
    public interface ISessionsApi
    {
        [Post("/api/sessions/topic/start")]
        Task<PracticeSessionDto> StartTopicPracticeAsync(StartTopicPracticeRequestDto request);

        [Post("/api/sessions/review/start")]
        Task<PracticeSessionDto> StartReviewAsync(StartLicensePracticeRequestDto request);

        [Post("/api/sessions/quick/start")]
        Task<PracticeSessionDto> StartQuickAsync(StartLicensePracticeRequestDto request);

        [Get("/api/dashboard")]
        Task<DashboardDto> GetDashboardAsync(long licenseId);

        [Post("/api/sessions/topic/reset")]
        Task ResetTopicPracticeAsync(ResetTopicPracticeRequestDto request);

        [Post("/api/sessions/{sessionId}/answers")]
        Task<SubmitAnswerResultDto> SubmitAnswerAsync(long sessionId, SubmitAnswerRequestDto request);
    }
}
