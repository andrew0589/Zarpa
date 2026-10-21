namespace NavigationES.Shared.Dtos
{
    // The Estudiar section (video lessons). A chapter is a topic of the user's
    // license; its videos are the manual's subsections (1.1, 1.2, 1.3a…).

    // Whether the section is open to this user: always for administrators, for
    // everyone once Study:Public is switched on in the API.
    public record StudyAccessDto(bool Enabled);

    public record StudyOverviewDto(List<StudyChapterDto> Chapters, StudyContinueDto? Continue);

    // CompletedCount of VideoCount watched to the end; TotalSeconds = sum of durations.
    public record StudyChapterDto(int Number, string Name, string? ImageUrl, int VideoCount, int TotalSeconds, int CompletedCount);

    // The video the user last left unfinished.
    public record StudyContinueDto(
        long VideoId, int TopicNumber, string TopicName, string Code, string Title,
        string? ImageUrl, int DurationSeconds, int PositionSeconds);

    public record StudyTopicDto(int Number, string Name, string? Description, string? ImageUrl, List<StudyVideoSummaryDto> Videos);

    // Locked = not free and the account has no trial, subscription or admin.
    // IsPublished is false only in an administrator's view (drafts).
    public record StudyVideoSummaryDto(
        long Id, string Code, string Title, string? Subtitle, string? ImageUrl, int DurationSeconds,
        bool IsFree, bool Locked, bool IsPublished, int PositionSeconds, bool Completed);

    // VideoUrl and SubtitlesUrl are null when Locked.
    public record StudyVideoDto(
        long Id, int TopicNumber, string TopicName, string Code, string Title, string? Subtitle,
        string? ImageUrl, string? VideoUrl, string? SubtitlesUrl, int DurationSeconds, bool Locked,
        int PositionSeconds, bool Completed, List<string> KeyTerms, List<StudyVideoChapterDto> Chapters,
        StudyVideoSummaryDto? Next);

    // Start in seconds from the beginning of the video.
    public record StudyVideoChapterDto(string Title, int Start);

    public record StudyProgressRequestDto(int PositionSeconds, bool Completed);

    // ---- Administration (the web's Vídeos tab) ----

    // Licenses = the codes whose blueprint contains the topic ("PNB · PER").
    public record AdminStudyTopicDto(
        long Id, int Number, string Name, string Licenses, string? ImageUrl, string? Description,
        List<AdminStudyVideoDto> Videos);

    public record AdminStudyVideoDto(
        long Id, string Code, int SortOrder, string Title, string? Subtitle, string? ImageUrl,
        string? VideoUrl, string? SubtitlesUrl, int DurationSeconds, bool IsFree, bool IsPublished,
        int KeyTermCount, int ChapterCount);

    public record AdminStudyTopicUpdateDto(string? ImageUrl, string? Description);

    public record AdminStudyVideoUpdateDto(
        string Title, string? Subtitle, string? ImageUrl, string? VideoUrl, string? SubtitlesUrl,
        int DurationSeconds, int SortOrder, bool IsFree, bool IsPublished);

    // One row of the catalogue file written by NavigationES Documentation/Videos/exporta_catalogo.py.
    // Upserted by (TopicNumber, Code). On an existing video a null ImageUrl, VideoUrl,
    // SubtitlesUrl, IsFree or IsPublished keeps what the administrator set; a new
    // video starts unpublished and not free unless the file says otherwise.
    public record StudyVideoImportDto(
        int TopicNumber, string Code, int SortOrder, string Title, string? Subtitle,
        string? ImageUrl, string? VideoUrl, string? SubtitlesUrl, int DurationSeconds,
        List<string>? KeyTerms, List<StudyVideoChapterDto>? Chapters, bool? IsFree, bool? IsPublished);

    public record StudyImportResultDto(int Created, int Updated, List<string> Errors);
}
