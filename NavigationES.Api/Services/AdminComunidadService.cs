using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    // Behind the web's Convocatorias tab: per comunidad autónoma, the most recent
    // sitting whose papers are in the database, plus three hand-kept fields — the
    // site publishing the convocatorias, the next sitting the administrator is
    // waiting for and free notes about it. Only those three are ever written; the
    // "last" side is derived from Exams.
    public class AdminComunidadService(NavigationESDbContext context)
    {
        // The column limits, kept in Shared so the web form knows them too.
        public const int UrlMaxLength = AdminComunidadLimits.UrlMaxLength;
        public const int NotesMaxLength = AdminComunidadLimits.NotesMaxLength;

        private readonly NavigationESDbContext _context = context;

        // One imported paper, flattened — the input of the pure grouping below.
        public record ExamPaper(
            long ComunidadId,
            long LicenseId,
            string LicenseCode,
            // Both null for an undated paper.
            int? Year,
            int? Month,
            string? Model,
            string? SourceFile,
            int QuestionCount);

        public async Task<List<AdminComunidadExamDto>> GetAsync()
        {
            var comunidades = await _context.ComunidadesAutonomas.AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(c => new { c.ID, c.Name, c.ConvocatoriaUrl, c.NextExamDate, c.Notes })
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
                .Select(c => Build(c.ID, c.Name, c.ConvocatoriaUrl, c.NextExamDate, c.Notes, byComunidad[c.ID]))
                .ToList();
        }

        public async Task<ResultDto> UpdateAsync(long comunidadId, AdminComunidadUpdateDto request)
        {
            var (url, urlError) = NormalizeUrl(request.ConvocatoriaUrl);
            if (urlError is not null)
                return ResultDto.Failure(urlError);

            var (notes, notesError) = NormalizeNotes(request.Notes);
            if (notesError is not null)
                return ResultDto.Failure(notesError);

            var comunidad = await _context.ComunidadesAutonomas.FirstOrDefaultAsync(c => c.ID == comunidadId);
            if (comunidad is null)
                return ResultDto.Failure(ErrorCodes.ComunidadNotFoundError);

            comunidad.ConvocatoriaUrl = url;
            comunidad.NextExamDate = request.NextExamDate;
            comunidad.Notes = notes;
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

        // Trims the ends and treats blank as "nothing written"; the line breaks the
        // administrator typed inside stay as they are. Error is null when it fits.
        public static (string? Notes, string? Error) NormalizeNotes(string? raw)
        {
            var notes = raw?.Trim();
            if (string.IsNullOrEmpty(notes))
                return (null, null);

            return notes.Length <= NotesMaxLength
                ? (notes, null)
                : (null, ErrorCodes.ConvocatoriaNotesTooLongError);
        }

        // Pure: one tab row from the community's papers. Sittings are ordered by
        // (Year, Month), so December 2025 comes before June 2026. Public and static
        // so the rules are unit-tested without a database.
        //
        // Undated papers (third-party reprints) still count in ExamCount — they are
        // in the database and the user can sit them — but they can never be "the last
        // imported sitting", so every "last" here is computed from the dated ones.
        public static AdminComunidadExamDto Build(
            long id, string name, string? convocatoriaUrl, DateOnly? nextExamDate, string? notes, IEnumerable<ExamPaper> papers)
        {
            var list = papers.ToList();
            var dated = list.Where(p => p.Year is not null && p.Month is not null).ToList();
            if (dated.Count == 0)
                return new AdminComunidadExamDto(id, name, list.Count, null, [], convocatoriaUrl, nextExamDate, notes);

            var latest = dated.Max(p => (p.Year, p.Month));
            var latestPapers = dated
                .Where(p => (p.Year, p.Month) == latest)
                .OrderBy(p => p.LicenseId).ThenBy(p => p.Model).ThenBy(p => p.SourceFile)
                .Select(p => new AdminLastExamPaperDto(p.LicenseCode, p.Model, p.SourceFile, p.QuestionCount))
                .ToList();

            var byLicense = dated
                .GroupBy(p => new { p.LicenseId, p.LicenseCode })
                .OrderBy(g => g.Key.LicenseId)
                .Select(g =>
                {
                    var last = g.Max(p => (p.Year, p.Month));
                    return new AdminLicenseLastExamDto(g.Key.LicenseCode, last.Year!.Value, last.Month!.Value, g.Count(p => (p.Year, p.Month) == last));
                })
                .ToList();

            return new AdminComunidadExamDto(
                id, name, list.Count,
                new AdminLastExamDto(latest.Year!.Value, latest.Month!.Value, latestPapers),
                byLicense,
                convocatoriaUrl, nextExamDate, notes);
        }
    }
}
