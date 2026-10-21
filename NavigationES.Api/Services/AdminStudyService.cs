using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;
using NavigationES.Api.Data.Entities;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    // Behind the web's Vídeos tab: every topic with all its videos (drafts too), the
    // chapter cover and blurb, per-video edits and the catalogue import written by
    // Documentation/Videos/exporta_catalogo.py.
    public partial class AdminStudyService(NavigationESDbContext context, IWebHostEnvironment environment)
    {
        public const int UrlMaxLength = 500;
        public const int DescriptionMaxLength = 1000;

        // The folder behind https://api…/videos/ — the `navigationes-videos` Docker
        // volume in production, NavigationES.Api/wwwroot/videos (gitignored) locally.
        public const string VideoFolder = "videos";

        private readonly NavigationESDbContext _context = context;
        private readonly IWebHostEnvironment _environment = environment;

        // "1.3a_equipo_fondeo.mp4": the names generar_video.py writes. No paths, no
        // spaces, nothing that could leave the folder.
        [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,149}\.(mp4|vtt)$", RegexOptions.IgnoreCase)]
        private static partial Regex UploadNameRegex();

        public static bool IsValidUploadName(string? name) =>
            name is not null && !name.Contains("..") && UploadNameRegex().IsMatch(name);

        // Stores one uploaded file under its own name, replacing a previous version.
        // Written to a temporary file first, so a viewer streaming the old video is
        // not served a half-written one.
        public async Task<ResultDto> SaveVideoFileAsync(string? fileName, Stream content)
        {
            var name = Path.GetFileName(fileName ?? string.Empty);
            if (!IsValidUploadName(name))
                return ResultDto.Failure(ErrorCodes.StudyFileNotValidError);

            var folder = Path.Combine(_environment.WebRootPath, VideoFolder);
            Directory.CreateDirectory(folder);
            var target = Path.Combine(folder, name);
            var temp = Path.Combine(folder, $".{Guid.NewGuid():N}.upload");

            try
            {
                long length;
                await using (var file = File.Create(temp))
                {
                    await content.CopyToAsync(file);
                    length = file.Length;
                }
                if (length == 0)
                    return ResultDto.Failure(ErrorCodes.StudyFileNotValidError);

                File.Move(temp, target, overwrite: true);
                return ResultDto.Success($"{VideoFolder}/{name}");
            }
            finally
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
        }

        public async Task<List<AdminStudyTopicDto>> GetAsync()
        {
            var topics = await _context.Topics.AsNoTracking().OrderBy(t => t.Number).ToListAsync();
            var licenses = await _context.LicenseTopics.AsNoTracking()
                .OrderBy(lt => lt.LicenseID)
                .Select(lt => new { lt.TopicID, lt.License.Code })
                .ToListAsync();
            var videos = await _context.StudyVideos.AsNoTracking()
                .OrderBy(v => v.SortOrder).ThenBy(v => v.Code)
                .ToListAsync();

            return topics
                .Select(t => new AdminStudyTopicDto(
                    t.ID, t.Number, t.Name,
                    string.Join(" · ", licenses.Where(l => l.TopicID == t.ID).Select(l => l.Code)),
                    t.StudyImageUrl, t.StudyDescription,
                    videos.Where(v => v.TopicID == t.ID)
                        .Select(v => new AdminStudyVideoDto(
                            v.ID, v.Code, v.SortOrder, v.Title, v.Subtitle, v.ImageUrl, v.VideoUrl, v.SubtitlesUrl,
                            v.DurationSeconds, v.IsFree, v.IsPublished,
                            StudyService.ParseKeyTerms(v.KeyTermsJson).Count, StudyService.ParseChapters(v.ChaptersJson).Count))
                        .ToList()))
                .ToList();
        }

        public async Task<ResultDto> UpdateTopicAsync(int number, AdminStudyTopicUpdateDto request)
        {
            var (image, imageOk) = NormalizeUrl(request.ImageUrl);
            var description = Blank(request.Description);
            if (!imageOk || description?.Length > DescriptionMaxLength)
                return ResultDto.Failure(ErrorCodes.StudyVideoNotValidError);

            var topic = await _context.Topics.FirstOrDefaultAsync(t => t.Number == number);
            if (topic is null)
                return ResultDto.Failure(ErrorCodes.StudyTopicNotFoundError);

            topic.StudyImageUrl = image;
            topic.StudyDescription = description;
            await _context.SaveChangesAsync();
            return ResultDto.Success();
        }

        public async Task<ResultDto> UpdateVideoAsync(long id, AdminStudyVideoUpdateDto request)
        {
            var title = Blank(request.Title);
            var subtitle = Blank(request.Subtitle);
            var (image, imageOk) = NormalizeUrl(request.ImageUrl);
            var (video, videoOk) = NormalizeUrl(request.VideoUrl);
            var (vtt, vttOk) = NormalizeUrl(request.SubtitlesUrl);
            if (title is null || title.Length > 150 || subtitle?.Length > 300 || !imageOk || !videoOk || !vttOk || request.DurationSeconds < 0)
                return ResultDto.Failure(ErrorCodes.StudyVideoNotValidError);

            var row = await _context.StudyVideos.FirstOrDefaultAsync(v => v.ID == id);
            if (row is null)
                return ResultDto.Failure(ErrorCodes.StudyVideoNotFoundError);

            row.Title = title;
            row.Subtitle = subtitle;
            row.ImageUrl = image;
            row.VideoUrl = video;
            row.SubtitlesUrl = vtt;
            row.DurationSeconds = request.DurationSeconds;
            row.SortOrder = request.SortOrder;
            row.IsFree = request.IsFree;
            row.IsPublished = request.IsPublished;
            row.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return ResultDto.Success();
        }

        // Progress rows go with the video (cascade).
        public async Task<ResultDto> DeleteVideoAsync(long id)
        {
            var deleted = await _context.StudyVideos.Where(v => v.ID == id).ExecuteDeleteAsync();
            return deleted > 0 ? ResultDto.Success() : ResultDto.Failure(ErrorCodes.StudyVideoNotFoundError);
        }

        // Upserts every valid row by (topic, code); invalid rows are reported and
        // skipped, the rest are saved together.
        public async Task<StudyImportResultDto> ImportAsync(List<StudyVideoImportDto> rows)
        {
            var topics = await _context.Topics.AsNoTracking().ToDictionaryAsync(t => t.Number, t => t.ID);
            var existing = await _context.StudyVideos.ToListAsync();
            int created = 0, updated = 0;
            var errors = new List<string>();

            foreach (var r in rows)
            {
                var label = $"{r.TopicNumber}/{r.Code}";
                var code = Blank(r.Code);
                var title = Blank(r.Title);
                var (image, imageOk) = NormalizeUrl(r.ImageUrl);
                var (video, videoOk) = NormalizeUrl(r.VideoUrl);
                var (vtt, vttOk) = NormalizeUrl(r.SubtitlesUrl);

                if (!topics.TryGetValue(r.TopicNumber, out var topicId))
                {
                    errors.Add($"{label}: el tema {r.TopicNumber} no existe.");
                    continue;
                }
                if (code is null || code.Length > 10 || title is null || title.Length > 150 || Blank(r.Subtitle)?.Length > 300
                    || !imageOk || !videoOk || !vttOk || r.DurationSeconds < 0)
                {
                    errors.Add($"{label}: datos no válidos (código, título o URLs).");
                    continue;
                }

                var row = existing.FirstOrDefault(v => v.TopicID == topicId && v.Code == code);
                if (row is null)
                {
                    row = new StudyVideoEntity { TopicID = topicId, Code = code };
                    _context.StudyVideos.Add(row);
                    existing.Add(row);
                    created++;
                }
                else
                {
                    updated++;
                }

                row.Title = title;
                row.Subtitle = Blank(r.Subtitle);
                row.SortOrder = r.SortOrder;
                row.DurationSeconds = r.DurationSeconds;
                row.ImageUrl = image ?? row.ImageUrl;
                row.VideoUrl = video ?? row.VideoUrl;
                row.SubtitlesUrl = vtt ?? row.SubtitlesUrl;
                row.IsFree = r.IsFree ?? row.IsFree;
                row.IsPublished = r.IsPublished ?? row.IsPublished;
                row.KeyTermsJson = r.KeyTerms is null ? row.KeyTermsJson : JsonSerializer.Serialize(r.KeyTerms);
                row.ChaptersJson = r.Chapters is null ? row.ChaptersJson : JsonSerializer.Serialize(r.Chapters, JsonSerializerOptions.Web);
                row.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return new StudyImportResultDto(created, updated, errors);
        }

        private static string? Blank(string? value)
        {
            var v = value?.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        // Blank = none. Accepted: an absolute http(s) URL, or a path relative to the
        // API (images/study/…, videos/…) without "..", within the column length.
        public static (string? Url, bool Ok) NormalizeUrl(string? raw)
        {
            var url = Blank(raw);
            if (url is null)
                return (null, true);
            if (url.Length > UrlMaxLength || url.Contains(".."))
                return (null, false);

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps ? (url, true) : (null, false);

            var relative = url.TrimStart('/');
            return Uri.IsWellFormedUriString(relative, UriKind.Relative) && relative.Length > 0 ? (relative, true) : (null, false);
        }
    }
}
