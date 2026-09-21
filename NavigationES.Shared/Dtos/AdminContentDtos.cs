namespace NavigationES.Shared.Dtos
{
    // The web's Contenido tab (administrators only): how much material each license
    // (titulación) has per topic, and how many exam simulations each comunidad
    // autónoma has per license. The per-license lists only carry the licenses that
    // apply — a missing license means "topic not part of this license" or "no exams".

    public record AdminLicenseDto(
        long Id,
        string Code,
        string Name,
        int TopicCount,
        // Sum of QuestionsInExam over the license's topics: the size of one exam.
        int QuestionsInExam,
        int? MaxTotalErrors,
        // Active questions in the bank across the license's topics.
        int BankQuestionCount,
        // Imported exam simulations of this license, all comunidades together.
        int ExamCount);

    public record AdminTopicLicenseDto(long LicenseId, int QuestionsInExam, int? MaxErrors);

    public record AdminTopicDto(
        long Id,
        int Number,
        string Name,
        int ActiveQuestionCount,
        int InactiveQuestionCount,
        List<AdminTopicLicenseDto> Licenses);

    // FirstYear/LastYear are null when every paper of this comunidad+license is undated
    // (SQL's MIN/MAX skip the nulls, so a mixed set still reports the dated span).
    public record AdminComunidadLicenseDto(long LicenseId, int ExamCount, int QuestionCount, int? FirstYear, int? LastYear);

    public record AdminComunidadDto(long Id, string Name, List<AdminComunidadLicenseDto> Licenses);

    public record AdminContentDto(
        List<AdminLicenseDto> Licenses,
        List<AdminTopicDto> Topics,
        List<AdminComunidadDto> Comunidades);
}
