namespace Zarpa.Shared.Dtos
{
    public record QuestionImportResultDto(
        int Imported,
        int SkippedDuplicates,
        // Duplicates that filled in a missing explanation/image on the existing question.
        int EnrichedExisting,
        // One entry per skipped duplicate, naming the question and why it was skipped.
        List<string> Duplicates,
        // Same statement as an existing question but a different correct answer — review by hand.
        List<string> Suspects,
        List<string> Errors);
}
