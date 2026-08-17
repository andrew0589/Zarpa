namespace Zarpa.Shared.Dtos
{
    // One question to import. CorrectIndex is 1-based (1–4), matching answers a/b/c/d.
    public record QuestionImportDto(
        int TopicNumber,
        string Text,
        List<string> Answers,
        int CorrectIndex,
        string? Explanation = null,
        string? ExplanationImageUrl = null,
        string? SourceExam = null);
}
