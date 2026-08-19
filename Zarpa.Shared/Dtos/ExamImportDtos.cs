namespace Zarpa.Shared.Dtos
{
    // One full official exam paper, questions in the order of the sheet. Comunidad is
    // the community's seeded name ("Islas Baleares"), Categoria the license code ("PER").
    public record ExamImportDto(
        string Comunidad,
        string Categoria,
        int Year,
        int Month,
        string? Model,
        string? SourceFile,
        List<QuestionImportDto> Questions);

    // The paper is stored verbatim and independently of the topic bank. All-or-nothing:
    // when Errors is non-empty nothing was saved. Every import inserts a NEW exam —
    // duplicates are allowed by design and cleaned up by the admin afterwards.
    public record ExamImportResultDto(
        bool Saved,
        long ExamId,
        int TotalQuestions,
        int Inserted,
        List<string> Errors);
}
