namespace Zarpa.Shared.Dtos
{
    // One question as shown DURING the simulation — no correct flag anywhere;
    // ChosenIndex carries the user's current answer when resuming.
    public record ExamSessionQuestionDto(
        long Id,
        int Position,
        string Text,
        List<string> Answers,
        string? QuestionImageUrl,
        int? ChosenIndex);

    // RemainingSeconds is null for untimed licenses; the client counts down from it.
    public record StartExamSessionDto(
        long SessionId,
        int TotalQuestions,
        int? RemainingSeconds,
        List<ExamSessionQuestionDto> Questions);

    public record SubmitExamAnswerRequestDto(long ExamQuestionId, int ChosenIndex);

    // Review data, revealed only after finishing.
    public record ExamResultQuestionDto(
        long ExamQuestionId,
        int Position,
        int TopicNumber,
        string TopicName,
        string Text,
        List<string> Answers,
        int CorrectIndex,
        int? ChosenIndex);

    public record ExamTopicResultDto(
        int TopicNumber,
        string TopicName,
        int Questions,
        int Errors,
        // Per-topic error limit from the license blueprint; null = no own limit.
        int? MaxErrors,
        bool WithinLimit);

    public record ExamSessionResultDto(
        long SessionId,
        bool Passed,
        int TotalQuestions,
        int Correct,
        int Wrong,
        int Unanswered,
        // Errors = wrong + unanswered — what the official correction counts.
        int TotalErrors,
        int? MaxTotalErrors,
        List<ExamTopicResultDto> Topics,
        List<ExamResultQuestionDto> Questions);
}
