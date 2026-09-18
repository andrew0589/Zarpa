using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    // Behind the web's Convocatorias tab: per comunidad autónoma, the most recent
    // sitting whose papers are in the database, plus three hand-kept fields — the
    // site publishing the convocatorias, the next sitting the administrator is
    // waiting for and whether it is done. Only those three are ever written; the
    // "last" side is derived from Exams.
    public class AdminComunidadService(NavigationESDbContext context)
    {
        // Matches [MaxLength] on ComunidadAutonomaEntity.ConvocatoriaUrl.
        public const int UrlMaxLength = 500;

        private readonly NavigationESDbContext _context = context;

        // One imported paper, flattened — the input of the pure grouping below.
        public record ExamPaper(
            long ComunidadId,
            long LicenseId,
            string LicenseCode,
            int Year,
            int Month,
            string? Model,
            string? SourceFile,
            int QuestionCount);

        public async Task<List<AdminComunidadExamDto>> GetAsync()
        {
            var comunidades = await _context.ComunidadesAutonomas.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.ID, c.Name, c.ConvocatoriaUrl, c.NextExamDate, c.NextExamDone })
                .ToListAsync();

            // A few hundred rows in total, so the per-community grouping happens in memory.
            var papers = await _context.Exams.AsNoTracking()
                .Select(e => new
                {
                    e.ComunidadAutonomaID,
                    e.LicenseID,
                    e.License.Code,
                    e.Year,
                    e.Month,
                    e.Model,
                    e.SourceFile,
                    QuestionCount = _context.ExamQuestions.Count(q => q.ExamID == e.ID),
                })
                .ToListAsync();

            var byComunidad = papers
                .Select(p => new ExamPaper(p.ComunidadAutonomaID, p.LicenseID, p.Code, p.Year, p.Month, p.Model, p.SourceFile, p.QuestionCount))
                .ToLookup(p => p.ComunidadId);

            return comunidades
                .Select(c => Build(c.ID, c.Name, c.ConvocatoriaUrl, c.NextExamDate, c.NextExamDone, byComunidad[c.ID]))
                .ToList();
        }

        public async Task<ResultDto> UpdateAsync(long comunidadId, AdminComunidadUpdateDto request)
        {
            var (url, error) = NormalizeUrl(request.ConvocatoriaUrl);
            if (error is not null)
                return ResultDto.Failure(error);

            var comunidad = await _context.ComunidadesAutonomas.FirstOrDefaultAsync(c => c.ID == comunidadId);
            if (comunidad is null)
                return ResultDto.Failure(ErrorCodes.ComunidadNotFoundError);

            comunidad.ConvocatoriaUrl = url;
            comunidad.NextExamDate = request.NextExamDate;
            comunidad.NextExamDone = request.NextExamDone;
            await _context.SaveChangesAsync();

            return ResultDto.Success();
        }

        // Trims, treats blank as "no link", and accepts only an absolute http(s) URL
        // that fits the column. Error is null when the value is acceptable.
        public static (string? Url, string? Error) NormalizeUrl(string? raw)
        {
            var url = raw?.Trim();
            if (string.IsNullOrEmpty(url))
                return (null, null);

            var valid = url.Length <= UrlMaxLength
                && Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

            return valid ? (url, null) : (null, ErrorCodes.ConvocatoriaUrlNotValidError);
        }

        // Pure: one tab row from the community's papers. Sittings are ordered by
        // (Year, Month), so December 2025 comes before June 2026. Public and static
        // so the rules are unit-tested without a database.
        public static AdminComunidadExamDto Build(
            long id, string name, string? convocatoriaUrl, DateOnly? nextExamDate, bool nextExamDone, IEnumerable<ExamPaper> papers)
        {
            var list = papers.ToList();
            if (list.Count == 0)
                return new AdminComunidadExamDto(id, name, 0, null, [], convocatoriaUrl, nextExamDate, nextExamDone);

            var latest = list.Max(p => (p.Year, p.Month));
            var latestPapers = list
                .Where(p => (p.Year, p.Month) == latest)
                .OrderBy(p => p.LicenseId).ThenBy(p => p.Model).ThenBy(p => p.SourceFile)
                .Select(p => new AdminLastExamPaperDto(p.LicenseCode, p.Model, p.SourceFile, p.QuestionCount))
                .ToList();

            var byLicense = list
                .GroupBy(p => new { p.LicenseId, p.LicenseCode })
                .OrderBy(g => g.Key.LicenseId)
                .Select(g =>
                {
                    var last = g.Max(p => (p.Year, p.Month));
                    return new AdminLicenseLastExamDto(g.Key.LicenseCode, last.Year, last.Month, g.Count(p => (p.Year, p.Month) == last));
                })
                .ToList();

            return new AdminComunidadExamDto(
                id, name, list.Count,
                new AdminLastExamDto(latest.Year, latest.Month, latestPapers),
                byLicense,
                convocatoriaUrl, nextExamDate, nextExamDone);
        }
    }
}
