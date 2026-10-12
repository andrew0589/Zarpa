using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;

namespace NavigationES.Api.Services
{
    // The data behind the public, crawlable pages (PublicSiteEndpoints): the exam
    // calendar per comunidad autónoma, what official papers each one has in the
    // database, a few sample questions and the topic pages. Everything here is
    // read-only and anonymous, so it only ever exposes what a visitor may see
    // before signing up — counts, dates and a handful of questions.
    public class PublicSiteService(NavigationESDbContext context)
    {
        private readonly NavigationESDbContext _context = context;

        // How many questions a public page shows; the rest stay behind the account.
        public const int SampleQuestionCount = 6;

        public record LicenseSummary(string Code, string Name, int ExamCount, int QuestionCount, int? FirstYear, int? LastYear, int? LastMonth);

        public record ComunidadSummary(
            long Id,
            string Name,
            string Slug,
            string? ConvocatoriaUrl,
            DateOnly? NextExamDate,
            int ExamCount,
            int QuestionCount,
            List<LicenseSummary> Licenses);

        public record SampleQuestion(string Text, string[] Answers, int CorrectIndex, string? ImageUrl, string? Explanation, string? ExplanationImageUrl);

        public record SampleExam(string LicenseCode, string LicenseName, int? Year, int? Month, string? Model, List<SampleQuestion> Questions);

        public record TopicSummary(long Id, int Number, string Name, string Slug, int QuestionCount, List<(string LicenseCode, int QuestionsInExam, int? MaxErrors)> InExams);

        public async Task<List<ComunidadSummary>> GetComunidadesAsync()
        {
            var comunidades = await _context.ComunidadesAutonomas.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.ID, c.Name, c.ConvocatoriaUrl, c.NextExamDate })
                .ToListAsync();

            var papers = await _context.Exams.AsNoTracking()
                .Select(e => new
                {
                    e.ComunidadAutonomaID,
                    e.LicenseID,
                    e.License.Code,
                    e.License.Name,
                    e.Year,
                    e.Month,
                    QuestionCount = _context.ExamQuestions.Count(q => q.ExamID == e.ID),
                })
                .ToListAsync();

            var byComunidad = papers.ToLookup(p => p.ComunidadAutonomaID);

            return comunidades.Select(c =>
            {
                var own = byComunidad[c.ID].ToList();
                var licenses = own
                    .GroupBy(p => new { p.LicenseID, p.Code, p.Name })
                    .OrderBy(g => g.Key.LicenseID)
                    .Select(g =>
                    {
                        var dated = g.Where(p => p.Year is not null && p.Month is not null).ToList();
                        var last = dated.Count == 0 ? ((int?)null, (int?)null) : dated.Max(p => (p.Year, p.Month));
                        return new LicenseSummary(
                            g.Key.Code, g.Key.Name, g.Count(), g.Sum(p => p.QuestionCount),
                            dated.Count == 0 ? null : dated.Min(p => p.Year),
                            last.Item1, last.Item2);
                    })
                    .ToList();

                return new ComunidadSummary(
                    c.ID, c.Name, PublicSitePages.Slug(c.Name), c.ConvocatoriaUrl, c.NextExamDate,
                    own.Count, own.Sum(p => p.QuestionCount), licenses);
            }).ToList();
        }

        // A few questions of the community's most recent dated paper, PER first
        // (the most searched qualification), one per topic so the sample shows the
        // breadth of the exam rather than six balizamiento questions in a row.
        public async Task<SampleExam?> GetSampleExamAsync(long comunidadId)
        {
            var exam = await _context.Exams.AsNoTracking()
                .Where(e => e.ComunidadAutonomaID == comunidadId && e.Year != null && e.Month != null)
                .OrderByDescending(e => e.License.Code == "PER")
                .ThenByDescending(e => e.Year).ThenByDescending(e => e.Month).ThenBy(e => e.Model)
                .Select(e => new { e.ID, e.License.Code, e.License.Name, e.Year, e.Month, e.Model })
                .FirstOrDefaultAsync();
            if (exam is null)
                return null;

            var questions = await _context.ExamQuestions.AsNoTracking()
                .Where(q => q.ExamID == exam.ID)
                .OrderBy(q => q.Position)
                .Select(q => new { q.TopicID, q.Text, q.Answer1, q.Answer2, q.Answer3, q.Answer4, q.CorrectIndex, q.QuestionImageUrl })
                .ToListAsync();

            var sample = questions
                .GroupBy(q => q.TopicID).Select(g => g.First())
                .Concat(questions)
                .Distinct()
                .Take(SampleQuestionCount)
                .Select(q => new SampleQuestion(q.Text, [q.Answer1, q.Answer2, q.Answer3, q.Answer4], q.CorrectIndex, q.QuestionImageUrl, null, null))
                .ToList();

            return new SampleExam(exam.Code, exam.Name, exam.Year, exam.Month, exam.Model, sample);
        }

        // Topics that have enough questions in the bank to make a page worth reading.
        public async Task<List<TopicSummary>> GetTopicsAsync()
        {
            var topics = await _context.Topics.AsNoTracking()
                .OrderBy(t => t.Number)
                .Select(t => new
                {
                    t.ID,
                    t.Number,
                    t.Name,
                    QuestionCount = _context.Questions.Count(q => q.TopicID == t.ID && q.IsActive),
                })
                .ToListAsync();

            var blueprint = await _context.LicenseTopics.AsNoTracking()
                .OrderBy(lt => lt.LicenseID)
                .Select(lt => new { lt.TopicID, lt.License.Code, lt.QuestionsInExam, lt.MaxErrors })
                .ToListAsync();
            var byTopic = blueprint.ToLookup(b => b.TopicID);

            return topics
                .Where(t => t.QuestionCount >= SampleQuestionCount)
                .Select(t => new TopicSummary(
                    t.ID, t.Number, t.Name, PublicSitePages.Slug(t.Name), t.QuestionCount,
                    byTopic[t.ID].Select(b => (b.Code, b.QuestionsInExam, b.MaxErrors)).ToList()))
                .ToList();
        }

        // Bank questions of one topic, those with an explanation figure first: they
        // make the best page (and the figures can rank in image search).
        public async Task<List<SampleQuestion>> GetTopicSampleAsync(long topicId)
        {
            var questions = await _context.Questions.AsNoTracking()
                .Where(q => q.TopicID == topicId && q.IsActive)
                .OrderByDescending(q => q.ExplanationImageUrl != null)
                .ThenByDescending(q => q.Explanation != null)
                .ThenBy(q => q.ID)
                .Take(SampleQuestionCount)
                .Select(q => new
                {
                    q.Text,
                    q.QuestionImageUrl,
                    q.Explanation,
                    q.ExplanationImageUrl,
                    Answers = q.Answers.OrderBy(a => a.Order).Select(a => new { a.Text, a.IsCorrect }).ToList(),
                })
                .ToListAsync();

            return questions
                .Where(q => q.Answers.Count == 4)
                .Select(q => new SampleQuestion(
                    q.Text,
                    q.Answers.Select(a => a.Text).ToArray(),
                    q.Answers.FindIndex(a => a.IsCorrect) + 1,
                    q.QuestionImageUrl, q.Explanation, q.ExplanationImageUrl))
                .ToList();
        }
    }
}
