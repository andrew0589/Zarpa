using System.ComponentModel.DataAnnotations;

namespace NavigationES.Api.Data.Entities
{
    public class QuestionEntity
    {
        [Key]
        public long ID { get; set; }

        public long TopicID { get; set; }
        public TopicEntity Topic { get; set; }

        [Required]
        public string Text { get; set; }

        // SHA-256 of the normalized statement (QuestionTextNormalizer). Unique-indexed:
        // the same question imported twice — from any PDF, any time — is rejected by
        // the database. Text itself stays untouched for display.
        [Required, MaxLength(64)]
        public string ContentHash { get; set; }

        // Optional figure that is part of the STATEMENT (balizamiento/lights figures on
        // real papers) — shown before answering. Relative path under wwwroot.
        [MaxLength(500)]
        public string? QuestionImageUrl { get; set; }

        // Optional figure shown WITH the explanation, only after answering.
        // Relative path under wwwroot, e.g. "images/questions/x.png".
        [MaxLength(500)]
        public string? ExplanationImageUrl { get; set; }

        // HTML shown in the learning modes.
        public string? Explanation { get; set; }

        // Which official exam sittings the question appeared in, accumulated by the
        // importer, e.g. "PER abril 2023; PER octubre 2024".
        [MaxLength(300)]
        public string? SourceExam { get; set; }

        // Outdated questions (e.g. legislation changes) are deactivated, never deleted,
        // so users' answer history stays valid.
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<AnswerEntity> Answers { get; set; } = [];
    }
}
