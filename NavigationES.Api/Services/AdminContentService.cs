using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    // Behind the web's Contenido tab: the question bank per topic and license, and
    // the imported exam simulations per comunidad autónoma. Read-only. A handful of
    // small aggregate queries stitched together in memory — the whole catalogue is
    // a few hundred rows, so nothing here is paged.
    public class AdminContentService(NavigationESDbContext context)
    {
        private readonly NavigationESDbContext _context = context;

        public async Task<AdminContentDto> GetAsync()
        {
            var licenses = await _context.Licenses.AsNoTracking()
                .OrderBy(l => l.ID)
                .Select(l => new { l.ID, l.Code, l.Name, l.MaxTotalErrors })
                .ToListAsync();

            // The exam blueprint: which topics each license draws from, and how many.
            var blueprint = await _context.LicenseTopics.AsNoTracking()
                .Select(lt => new { lt.LicenseID, lt.TopicID, lt.QuestionsInExam, lt.MaxErrors })
                .ToListAsync();

            var topics = await _context.Topics.AsNoTracking()
                .OrderBy(t => t.Number).ThenBy(t => t.ID)
                .Select(t => new
                {
                    t.ID,
                    t.Number,
                    t.Name,
                    Active = _context.Questions.Count(q => q.TopicID == t.ID && q.IsActive),
                    Inactive = _context.Questions.Count(q => q.TopicID == t.ID && !q.IsActive),
                })
                .ToListAsync();

            var exams = await _context.Exams.AsNoTracking()
                .GroupBy(e => new { e.ComunidadAutonomaID, e.LicenseID })
                .Select(g => new
                {
                    g.Key.ComunidadAutonomaID,
                    g.Key.LicenseID,
                    ExamCount = g.Count(),
                    FirstYear = g.Min(e => e.Year),
                    LastYear = g.Max(e => e.Year),
                })
                .ToListAsync();

            var examQuestions = await _context.ExamQuestions.AsNoTracking()
                .GroupBy(q => new { q.Exam.ComunidadAutonomaID, q.Exam.LicenseID })
                .Select(g => new { g.Key.ComunidadAutonomaID, g.Key.LicenseID, Count = g.Count() })
                .ToListAsync();

            var comunidades = await _context.ComunidadesAutonomas.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.ID, c.Name })
                .ToListAsync();

            var activeByTopic = topics.ToDictionary(t => t.ID, t => t.Active);
            var questionsByGroup = examQuestions.ToDictionary(x => (x.ComunidadAutonomaID, x.LicenseID), x => x.Count);

            var licenseDtos = licenses.Select(l =>
            {
                var rows = blueprint.Where(b => b.LicenseID == l.ID).ToList();
                return new AdminLicenseDto(
                    l.ID, l.Code, l.Name,
                    rows.Count,
                    rows.Sum(b => b.QuestionsInExam),
                    l.MaxTotalErrors,
                    rows.Sum(b => activeByTopic.GetValueOrDefault(b.TopicID)),
                    exams.Where(e => e.LicenseID == l.ID).Sum(e => e.ExamCount));
            }).ToList();

            var topicDtos = topics.Select(t => new AdminTopicDto(
                t.ID, t.Number, t.Name, t.Active, t.Inactive,
                blueprint.Where(b => b.TopicID == t.ID)
                    .OrderBy(b => b.LicenseID)
                    .Select(b => new AdminTopicLicenseDto(b.LicenseID, b.QuestionsInExam, b.MaxErrors))
                    .ToList()))
                .ToList();

            var comunidadDtos = comunidades.Select(c => new AdminComunidadDto(
                c.ID, c.Name,
                exams.Where(e => e.ComunidadAutonomaID == c.ID)
                    .OrderBy(e => e.LicenseID)
                    .Select(e => new AdminComunidadLicenseDto(
                        e.LicenseID,
                        e.ExamCount,
                        questionsByGroup.GetValueOrDefault((c.ID, e.LicenseID)),
                        e.FirstYear,
                        e.LastYear))
                    .ToList()))
                .ToList();

            return new AdminContentDto(licenseDtos, topicDtos, comunidadDtos);
        }
    }
}
