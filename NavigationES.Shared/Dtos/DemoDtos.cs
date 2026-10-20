namespace NavigationES.Shared.Dtos
{
    // The no-account mini test on /prueba (GET /api/demo). Only questions the
    // public /test/{slug} pages already show, so the correct answer travels with
    // the question and the page checks it without a session. CorrectIndex is 1-based.
    public record DemoTestDto(string Title, string LicenseCode, string? TopicSlug, List<DemoQuestionDto> Questions);

    public record DemoQuestionDto(
        string Topic,
        string Text,
        string[] Answers,
        int CorrectIndex,
        string? ImageUrl,
        string? Explanation,
        string? ExplanationImageUrl);
}
