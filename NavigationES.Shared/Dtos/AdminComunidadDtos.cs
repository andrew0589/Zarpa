namespace NavigationES.Shared.Dtos
{
    // The web's Convocatorias tab (administrators only): for every comunidad autónoma,
    // the most recent sitting whose papers are imported, the site where the next
    // convocatoria is published, and the next sitting the administrator is waiting
    // for, with a tick once that one has been imported too.

    // One paper of the latest imported sitting; SourceFile is the PDF it came from.
    public record AdminLastExamPaperDto(string LicenseCode, string? Model, string? SourceFile, int QuestionCount);

    // The latest sitting across every license of the community, and its papers.
    public record AdminLastExamDto(int Year, int Month, List<AdminLastExamPaperDto> Papers);

    // The latest sitting of ONE license — a license can lag behind the others
    // (e.g. CY imported up to December while PER already has June).
    public record AdminLicenseLastExamDto(string LicenseCode, int Year, int Month, int ExamCount);

    public record AdminComunidadExamDto(
        long Id,
        string Name,
        // All imported papers of the community, every license together.
        int ExamCount,
        // Null when the community has no imported exams.
        AdminLastExamDto? LastExam,
        List<AdminLicenseLastExamDto> LastByLicense,
        // Where the community publishes convocatorias and exam papers; null = not set.
        string? ConvocatoriaUrl,
        DateOnly? NextExamDate,
        bool NextExamDone);

    // PUT /api/admin/comunidades/{id} — the three hand-kept fields, stored as sent:
    // a null date or a blank URL clears that field.
    public record AdminComunidadUpdateDto(DateOnly? NextExamDate, bool NextExamDone, string? ConvocatoriaUrl);
}
