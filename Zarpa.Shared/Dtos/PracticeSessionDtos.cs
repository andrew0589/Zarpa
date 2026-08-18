namespace Zarpa.Shared.Dtos
{
    public record StartTopicPracticeRequestDto(int TopicNumber, long LicenseId);

    public record ResetTopicPracticeRequestDto(int TopicNumber, long LicenseId);

    // An answer option as shown to the user — deliberately without the correct flag;
    // correctness comes back only after answering, from the submit endpoint.
    public record SessionAnswerOptionDto(long Id, string Text);

    // Questions never carry images; the explanation figure arrives only with the
    // answer result, so nothing is spoiled before answering.
    public record SessionQuestionDto(long Id, string Text, List<SessionAnswerOptionDto> Answers);

    // AnsweredCount lets the client resume mid-session: the first remaining question
    // is shown as "Pregunta AnsweredCount+1 de TotalQuestions".
    public record PracticeSessionDto(long SessionId, int TotalQuestions, int AnsweredCount, List<SessionQuestionDto> RemainingQuestions);

    public record SubmitAnswerRequestDto(long QuestionId, long AnswerId);

    public record SubmitAnswerResultDto(bool IsCorrect, long CorrectAnswerId, string? Explanation, string? ExplanationImageUrl, bool SessionFinished);
}
