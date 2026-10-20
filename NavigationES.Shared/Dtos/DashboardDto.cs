namespace NavigationES.Shared.Dtos
{
    // What the Inicio page needs beyond the topic and exam progress it already reads.
    // LastTopic: the topic of the user's most recent topic-practice session for this
    // license (null = never practiced). FailedCount: questions of the license whose
    // latest answer was wrong — what "Repasar mis fallos" covers. MyExamDate: the
    // date the user set for their own sitting (may be in the past: the page then asks
    // for the next one). NextExam: the next official sitting of the user's community,
    // as typed in the admin Convocatorias tab — only offered as a suggestion.
    public record DashboardDto(
        DashboardTopicDto? LastTopic,
        int FailedCount,
        DashboardExamDateDto? NextExam,
        int QuickTestSize,
        DateOnly? MyExamDate = null);

    // Answered/Total describe the open session; when the last session is finished,
    // Answered == Total and the client offers to start the topic again.
    public record DashboardTopicDto(int Number, string Name, int Answered, int Total, bool Finished);

    public record DashboardExamDateDto(string Comunidad, DateOnly Date);
}
