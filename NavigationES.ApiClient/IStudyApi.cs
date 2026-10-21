using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
{
    // The Estudiar section (video lessons). Until the API's Study:Public is on, only
    // administrators get past /access — everyone else gets 404 from the rest.
    public interface IStudyApi
    {
        [Get("/api/study/access")]
        Task<StudyAccessDto> GetAccessAsync();

        // The license's topics as chapters, plus the video to continue.
        [Get("/api/study")]
        Task<StudyOverviewDto> GetOverviewAsync(long licenseId);

        [Get("/api/study/topics/{number}")]
        Task<StudyTopicDto> GetTopicAsync(int number);

        [Get("/api/study/videos/{id}")]
        Task<StudyVideoDto> GetVideoAsync(long id);

        [Put("/api/study/videos/{id}/progress")]
        Task SaveProgressAsync(long id, [Body] StudyProgressRequestDto request);
    }
}
