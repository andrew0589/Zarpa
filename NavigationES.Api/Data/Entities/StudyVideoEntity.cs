using System.ComponentModel.DataAnnotations;

namespace NavigationES.Api.Data.Entities
{
    // One video lesson of the Estudiar section: a subsection of a topic's manual
    // (1.1 El casco, 1.3a Equipo de fondeo…). The topic is the chapter, so a license
    // sees the videos of exactly the topics in its blueprint (LicenseTopicEntity).
    public class StudyVideoEntity
    {
        [Key]
        public long ID { get; set; }

        public long TopicID { get; set; }
        public TopicEntity Topic { get; set; }

        // The subsection code shown to the user and the import's upsert key within
        // the topic: "1.1", "1.3a". Unique per topic.
        [Required, MaxLength(10)]
        public string Code { get; set; }

        // Order inside the topic; ties fall back to Code.
        public int SortOrder { get; set; }

        [Required, MaxLength(150)]
        public string Title { get; set; }

        [MaxLength(300)]
        public string? Subtitle { get; set; }

        // 16:9 thumbnail, also the player's poster. Relative to the API (images/study/…)
        // or absolute.
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        // The playable file or stream (mp4 or HLS). Null = not uploaded yet.
        [MaxLength(500)]
        public string? VideoUrl { get; set; }

        // WebVTT subtitles.
        [MaxLength(500)]
        public string? SubtitlesUrl { get; set; }

        public int DurationSeconds { get; set; }

        // Watchable without trial or subscription (the first video of each topic).
        public bool IsFree { get; set; }

        // Unpublished videos are visible to administrators only.
        public bool IsPublished { get; set; }

        // JSON array of strings: the key terms listed under the player.
        public string? KeyTermsJson { get; set; }

        // JSON array of { "title", "start" } (start in seconds): the video's chapters.
        public string? ChaptersJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
