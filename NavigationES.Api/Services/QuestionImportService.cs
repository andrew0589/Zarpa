using Markdig;
using NavigationES.Api.Data.Entities;
using NavigationES.Api.Data.Repositories;
using NavigationES.Api.Utilities.Questions;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    // Inserts imported questions with duplicate protection: an incoming statement whose
    // normalized hash already exists is skipped (its extra exam source is recorded), and
    // one that exists with a DIFFERENT correct answer is reported as suspect instead of
    // being silently resolved either way.
    public class QuestionImportService(ITopicRepository topicRepository, IQuestionRepository questionRepository)
    {
        private readonly ITopicRepository _topicRepository = topicRepository;
        private readonly IQuestionRepository _questionRepository = questionRepository;

        public async Task<QuestionImportResultDto> ImportAsync(List<QuestionImportDto> questions)
        {
            var topicsByNumber = await _topicRepository.GetByNumberAsync();

            var imported = 0;
            var skippedDuplicates = 0;
            var enrichedExisting = 0;
            var duplicates = new List<string>();
            var suspects = new List<string>();
            var errors = new List<string>();
            var hashesInBatch = new HashSet<string>();

            for (var i = 0; i < questions.Count; i++)
            {
                var dto = questions[i];
                var label = $"#{i + 1} \"{Truncate(dto.Text)}\"";

                if (string.IsNullOrWhiteSpace(dto.Text))
                {
                    errors.Add($"{label}: empty question text.");
                    continue;
                }
                if (!topicsByNumber.TryGetValue(dto.TopicNumber, out var topic))
                {
                    errors.Add($"{label}: unknown topicNumber {dto.TopicNumber} (expected 1–11).");
                    continue;
                }
                if (dto.Answers is not { Count: 4 } || dto.Answers.Any(string.IsNullOrWhiteSpace))
                {
                    errors.Add($"{label}: exactly 4 non-empty answers are required.");
                    continue;
                }
                if (dto.CorrectIndex is < 1 or > 4)
                {
                    errors.Add($"{label}: correctIndex must be between 1 and 4.");
                    continue;
                }

                var hash = QuestionTextNormalizer.ComputeHash(dto.Text);

                if (!hashesInBatch.Add(hash))
                {
                    skippedDuplicates++;
                    duplicates.Add($"{label}: appears twice in this batch.");
                    continue;
                }

                var existing = await _questionRepository.FindByContentHashAsync(hash);

                if (existing is not null)
                {
                    var existingCorrect = existing.Answers.FirstOrDefault(a => a.IsCorrect)?.Text ?? string.Empty;
                    var incomingCorrect = dto.Answers[dto.CorrectIndex - 1];

                    if (QuestionTextNormalizer.Normalize(existingCorrect) != QuestionTextNormalizer.Normalize(incomingCorrect))
                    {
                        suspects.Add($"{label}: same statement as question ID {existing.ID} but a different correct answer — review manually.");
                        continue;
                    }

                    // Re-sending a known question with content the bank is missing
                    // completes it — this is how explanations/images are added later.
                    var enriched = Enrich(existing, dto);
                    if (enriched)
                        enrichedExisting++;

                    AppendSource(existing, dto.SourceExam);
                    skippedDuplicates++;
                    duplicates.Add(enriched
                        ? $"{label}: already in the bank (question ID {existing.ID}) — missing fields completed."
                        : $"{label}: already in the bank (question ID {existing.ID}).");
                    continue;
                }

                var question = new QuestionEntity
                {
                    TopicID = topic.ID,
                    Text = dto.Text.Trim(),
                    ContentHash = hash,
                    QuestionImageUrl = dto.QuestionImageUrl,
                    ExplanationImageUrl = dto.ExplanationImageUrl,
                    Explanation = ToExplanationHtml(dto.Explanation),
                    SourceExam = dto.SourceExam,
                    Answers = [.. dto.Answers.Select((text, index) => new AnswerEntity
                    {
                        Text = text.Trim(),
                        Order = index + 1,
                        IsCorrect = index + 1 == dto.CorrectIndex,
                    })],
                };

                _questionRepository.Add(question);
                imported++;
            }

            await _questionRepository.SaveChangesAsync();

            return new QuestionImportResultDto(imported, skippedDuplicates, enrichedExisting, duplicates, suspects, errors);
        }

        // Fills the fields the bank is missing from the incoming DTO. Shared with the
        // exam import so both channels complete questions the same way.
        internal static bool Enrich(QuestionEntity existing, QuestionImportDto dto)
        {
            var enriched = false;
            if (string.IsNullOrWhiteSpace(existing.Explanation) && !string.IsNullOrWhiteSpace(dto.Explanation))
            {
                existing.Explanation = ToExplanationHtml(dto.Explanation);
                enriched = true;
            }
            if (string.IsNullOrWhiteSpace(existing.ExplanationImageUrl) && !string.IsNullOrWhiteSpace(dto.ExplanationImageUrl))
            {
                existing.ExplanationImageUrl = dto.ExplanationImageUrl;
                enriched = true;
            }
            if (string.IsNullOrWhiteSpace(existing.QuestionImageUrl) && !string.IsNullOrWhiteSpace(dto.QuestionImageUrl))
            {
                existing.QuestionImageUrl = dto.QuestionImageUrl;
                enriched = true;
            }
            return enriched;
        }

        // A repeated question means "this one comes up often" — keep every exam sitting
        // it appeared in, as long as it fits the column.
        internal static void AppendSource(QuestionEntity existing, string? sourceExam)
        {
            if (string.IsNullOrWhiteSpace(sourceExam))
                return;
            if (existing.SourceExam?.Contains(sourceExam, StringComparison.OrdinalIgnoreCase) == true)
                return;

            var combined = existing.SourceExam is null ? sourceExam : $"{existing.SourceExam}; {sourceExam}";
            if (combined.Length <= 300)
                existing.SourceExam = combined;
        }

        // Explanations arrive in Markdown (easier to author) but are stored as HTML —
        // the display contract of the app. HTML input passes through unchanged.
        internal static string? ToExplanationHtml(string? explanation) =>
            string.IsNullOrWhiteSpace(explanation) ? null : Markdown.ToHtml(explanation).Trim();

        internal static string Truncate(string? text) =>
            string.IsNullOrEmpty(text) ? "" : text.Length <= 60 ? text : text[..60] + "…";
    }
}
