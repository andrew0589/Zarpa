using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
{
    public interface IExamsApi
    {
        // The account's community selection filters server-side; only the license
        // travels as a parameter.
        [Get("/api/exams")]
        Task<List<ExamListItemDto>> GetExamsAsync(long licenseId);

        [Post("/api/exams/{examId}/sessions/start")]
        Task<StartExamSessionDto> StartExamSessionAsync(long examId);

        [Post("/api/exams/sessions/{sessionId}/answers")]
        Task SubmitExamAnswerAsync(long sessionId, SubmitExamAnswerRequestDto request);

        [Post("/api/exams/sessions/{sessionId}/finish")]
        Task<ExamSessionResultDto> FinishExamSessionAsync(long sessionId);

        [Get("/api/exams/sessions/{sessionId}/result")]
        Task<ExamSessionResultDto> GetExamSessionResultAsync(long sessionId);

        [Delete("/api/exams/sessions/{sessionId}")]
        Task AbandonExamSessionAsync(long sessionId);
    }
}
