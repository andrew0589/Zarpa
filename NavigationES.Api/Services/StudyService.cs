using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;
using NavigationES.Api.Data.Entities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    // The Estudiar section for a signed-in user. Chapters are the topics in the
    // license's blueprint (LicenseTopics), so PNB sees topics 1–6, PER 1–11, PY 12–15
    // and CY 16–19 without any per-license video table. Administrators also see
    // unpublished videos; a non-free video is locked without trial or subscription.
    public class StudyService(NavigationESDbContext context, BillingService billing)
    {
        private readonly NavigationESDbContext _context = context;
        private readonly BillingService _billing = billing;

        public async Task<StudyOverviewDto> GetOverviewAsync(long userId, long licenseId)
        {
            var isAdmin = await IsAdminAsync(userId);

            var topics = await _context.LicenseTopics.AsNoTracking()
                .Where(lt => lt.LicenseID == licenseId)
                .OrderBy(lt => lt.Topic.Number)
                .Select(lt => new { lt.Topic.ID, lt.Topic.Number, lt.Topic.Name, lt.Topic.StudyImageUrl })
                .ToListAsync();
            var topicIds = topics.Select(t => t.ID).ToList();

            var videos = await VisibleVideos(isAdmin)
                .Where(v => topicIds.Contains(v.TopicID))
                .Select(v => new { v.ID, v.TopicID, v.DurationSeconds })
                .ToListAsync();
            var videoIds = videos.Select(v => v.ID).ToList();

            var progress = await _context.StudyVideoProgress.AsNoTracking()
                .Where(p => p.UserID == userId && videoIds.Contains(p.VideoID))
                .ToListAsync();
            var completed = progress.Where(p => p.Completed).Select(p => p.VideoID).ToHashSet();

            var chapters = topics
                .Select(t =>
                {
                    var own = videos.Where(v => v.TopicID == t.ID).ToList();
                    return new StudyChapterDto(
                        t.Number, t.Name, t.StudyImageUrl, own.Count,
                        own.Sum(v => v.DurationSeconds), own.Count(v => completed.Contains(v.ID)));
                })
                .ToList();

            // "Continuar viendo": the most recently touched video left mid-way.
            StudyContinueDto? next = null;
            var last = progress
                .Where(p => !p.Completed && p.PositionSeconds > 0)
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefault();
            if (last is not null)
            {
                next = await _context.StudyVideos.AsNoTracking()
                    .Where(v => v.ID == last.VideoID)
                    .Select(v => new StudyContinueDto(
                        v.ID, v.Topic.Number, v.Topic.Name, v.Code, v.Title, v.ImageUrl, v.DurationSeconds, last.PositionSeconds))
                    .FirstOrDefaultAsync();
            }

            return new StudyOverviewDto(chapters, next);
        }

        public async Task<StudyTopicDto?> GetTopicAsync(long userId, int topicNumber)
        {
            var topic = await _context.Topics.AsNoTracking().FirstOrDefaultAsync(t => t.Number == topicNumber);
            if (topic is null)
                return null;

            var isAdmin = await IsAdminAsync(userId);
            var hasAccess = await _billing.HasAccessAsync(userId);
            var videos = await LoadSummariesAsync(userId, topic.ID, isAdmin, hasAccess);

            return new StudyTopicDto(topic.Number, topic.Name, topic.StudyDescription, topic.StudyImageUrl, videos);
        }

        public async Task<StudyVideoDto?> GetVideoAsync(long userId, long videoId)
        {
            var isAdmin = await IsAdminAsync(userId);
            var video = await VisibleVideos(isAdmin)
                .Include(v => v.Topic)
                .FirstOrDefaultAsync(v => v.ID == videoId);
            if (video is null)
                return null;

            var hasAccess = await _billing.HasAccessAsync(userId);
            var locked = !video.IsFree && !hasAccess;

            var progress = await _context.StudyVideoProgress.AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserID == userId && p.VideoID == videoId);

            var siblings = await LoadSummariesAsync(userId, video.TopicID, isAdmin, hasAccess);
            var index = siblings.FindIndex(s => s.Id == videoId);
            var next = index >= 0 && index + 1 < siblings.Count ? siblings[index + 1] : null;

            return new StudyVideoDto(
                video.ID, video.Topic.Number, video.Topic.Name, video.Code, video.Title, video.Subtitle,
                video.ImageUrl,
                locked ? null : video.VideoUrl,
                locked ? null : video.SubtitlesUrl,
                video.DurationSeconds, locked,
                progress?.PositionSeconds ?? 0, progress?.Completed ?? false,
                ParseKeyTerms(video.KeyTermsJson), ParseChapters(video.ChaptersJson),
                next);
        }

        // False when the video is not visible to the user or is locked for them.
        public async Task<bool> SaveProgressAsync(long userId, long videoId, StudyProgressRequestDto request)
        {
            var isAdmin = await IsAdminAsync(userId);
            var video = await VisibleVideos(isAdmin)
                .Where(v => v.ID == videoId)
                .Select(v => new { v.IsFree, v.DurationSeconds })
                .FirstOrDefaultAsync();
            if (video is null || (!video.IsFree && !await _billing.HasAccessAsync(userId)))
                return false;

            var position = Math.Clamp(request.PositionSeconds, 0, Math.Max(video.DurationSeconds, 0));
            var row = await _context.StudyVideoProgress.FirstOrDefaultAsync(p => p.UserID == userId && p.VideoID == videoId);
            if (row is null)
            {
                row = new StudyVideoProgressEntity { UserID = userId, VideoID = videoId };
                _context.StudyVideoProgress.Add(row);
            }

            row.PositionSeconds = position;
            row.Completed |= request.Completed;
            row.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<List<StudyVideoSummaryDto>> LoadSummariesAsync(long userId, long topicId, bool isAdmin, bool hasAccess)
        {
            var videos = await VisibleVideos(isAdmin)
                .Where(v => v.TopicID == topicId)
                .OrderBy(v => v.SortOrder).ThenBy(v => v.Code)
                .ToListAsync();
            var ids = videos.Select(v => v.ID).ToList();

            var progress = await _context.StudyVideoProgress.AsNoTracking()
                .Where(p => p.UserID == userId && ids.Contains(p.VideoID))
                .ToDictionaryAsync(p => p.VideoID);

            return videos
                .Select(v =>
                {
                    progress.TryGetValue(v.ID, out var p);
                    return new StudyVideoSummaryDto(
                        v.ID, v.Code, v.Title, v.Subtitle, v.ImageUrl, v.DurationSeconds,
                        v.IsFree, !v.IsFree && !hasAccess, v.IsPublished,
                        p?.PositionSeconds ?? 0, p?.Completed ?? false);
                })
                .ToList();
        }

        private IQueryable<StudyVideoEntity> VisibleVideos(bool isAdmin) =>
            _context.StudyVideos.AsNoTracking().Where(v => isAdmin || v.IsPublished);

        private Task<bool> IsAdminAsync(long userId) =>
            _context.Users.AsNoTracking().Where(u => u.ID == userId).Select(u => u.IsAdmin).FirstOrDefaultAsync();

        // Malformed JSON (a hand-edited row) shows as no terms/chapters rather than a 500.
        public static List<string> ParseKeyTerms(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return [];
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }

        public static List<StudyVideoChapterDto> ParseChapters(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return [];
            try
            {
                return JsonSerializer.Deserialize<List<StudyVideoChapterDto>>(json, JsonSerializerOptions.Web) ?? [];
            }
            catch (JsonException)
            {
                return [];
            }
        }
    }
}
