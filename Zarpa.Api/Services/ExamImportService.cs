using Zarpa.Api.Data.Entities;
using Zarpa.Api.Data.Repositories;
using Zarpa.Shared.Dtos;

namespace Zarpa.Api.Services
{
    // Imports one full official exam paper VERBATIM into its own tables, completely
    // independent of the deduplicated topic bank: no hash lookups, no enrichment, no
    // cross-paper conflicts. Papers repeat and reword questions between sittings —
    // that is expected and stored as-is; the volume (a few sittings per year) makes
    // the duplication irrelevant.
    //
    // All-or-nothing: any validation error aborts the whole save, so a paper is
    // either complete or absent. Every import inserts a NEW exam — duplicates are
    // allowed by design; the admin deduplicates afterwards (see the dedup SQL in
    // the project notes).
    public class ExamImportService(
        ITopicRepository topicRepository,
        ILicenseRepository licenseRepository,
        IExamRepository examRepository)
    {
        private readonly ITopicRepository _topicRepository = topicRepository;
        private readonly ILicenseRepository _licenseRepository = licenseRepository;
        private readonly IExamRepository _examRepository = examRepository;

        public async Task<ExamImportResultDto> ImportAsync(ExamImportDto dto)
        {
            var errors = new List<string>();

            var comunidad = await _examRepository.FindComunidadByNameAsync(dto.Comunidad ?? string.Empty);
            if (comunidad is null)
                errors.Add($"Unknown comunidad \"{dto.Comunidad}\" — must match a seeded name exactly (e.g. \"Islas Baleares\").");

            var license = await _licenseRepository.FindByCodeAsync(dto.Categoria ?? string.Empty);
            if (license is null)
                errors.Add($"Unknown categoria \"{dto.Categoria}\" — expected a license code (PNB/PER/PY/CY).");

            if (dto.Month is < 1 or > 12)
                errors.Add($"month must be 1–12, got {dto.Month}.");
            if (dto.Year is < 2000 or > 2100)
                errors.Add($"year looks wrong: {dto.Year}.");
            if (dto.Questions is not { Count: > 0 })
                errors.Add("questions is empty.");

            if (errors.Count > 0)
                return Failure(dto, errors);

            var model = string.IsNullOrWhiteSpace(dto.Model) ? null : dto.Model.Trim().ToUpperInvariant();
            var exam = new ExamEntity
            {
                ComunidadAutonomaID = comunidad!.ID,
                LicenseID = license!.ID,
                Year = dto.Year,
                Month = dto.Month,
                Model = model,
                SourceFile = dto.SourceFile,
            };
            _examRepository.AddExam(exam);

            var topicsByNumber = await _topicRepository.GetByNumberAsync();

            var position = 0;
            var inserted = 0;
            foreach (var q in dto.Questions)
            {
                position++;
                var label = $"#{position} \"{QuestionImportService.Truncate(q.Text)}\"";

                if (string.IsNullOrWhiteSpace(q.Text))
                {
                    errors.Add($"{label}: empty question text.");
                    continue;
                }
                if (!topicsByNumber.TryGetValue(q.TopicNumber, out var topic))
                {
                    errors.Add($"{label}: unknown topicNumber {q.TopicNumber} (expected 1–11).");
                    continue;
                }
                if (q.Answers is not { Count: 4 } || q.Answers.Any(string.IsNullOrWhiteSpace))
                {
                    errors.Add($"{label}: exactly 4 non-empty answers are required.");
                    continue;
                }
                if (q.CorrectIndex is < 1 or > 4)
                {
                    errors.Add($"{label}: correctIndex must be between 1 and 4.");
                    continue;
                }

                _examRepository.AddExamQuestion(new ExamQuestionEntity
                {
                    Exam = exam,
                    Position = position,
                    TopicID = topic.ID,
                    Text = q.Text.Trim(),
                    Answer1 = q.Answers[0].Trim(),
                    Answer2 = q.Answers[1].Trim(),
                    Answer3 = q.Answers[2].Trim(),
                    Answer4 = q.Answers[3].Trim(),
                    CorrectIndex = q.CorrectIndex,
                    QuestionImageUrl = q.QuestionImageUrl,
                });
                inserted++;
            }

            if (errors.Count > 0)
            {
                // The exam and its valid questions are already tracked — drop them so
                // they cannot ride along with a later exam's save (bulk import).
                _examRepository.DiscardChanges();
                return Failure(dto, errors);
            }

            await _examRepository.SaveChangesAsync();

            return new ExamImportResultDto(
                Saved: true,
                ExamId: exam.ID,
                TotalQuestions: dto.Questions.Count,
                Inserted: inserted,
                Errors: errors);
        }

        private static ExamImportResultDto Failure(ExamImportDto dto, List<string> errors) =>
            new(Saved: false, ExamId: 0,
                TotalQuestions: dto.Questions?.Count ?? 0,
                Inserted: 0, Errors: errors);
    }
}
