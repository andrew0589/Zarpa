namespace NavigationES.Shared.Dtos
{
    // One real exam paper in the simulation picker; the client groups by Year, with
    // the undated papers (Year and Month both null) in a "Sin fecha" group last.
    // Attempted = the user opened it at least once; Finished = at least one attempt
    // was graded; Passed = at least one graded attempt was apto.
    public record ExamListItemDto(
        long Id,
        int? Year,
        int? Month,
        string? Model,
        string ComunidadName,
        int QuestionCount,
        bool Attempted,
        bool Finished,
        bool Passed);
}
