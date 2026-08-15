using System.ComponentModel.DataAnnotations;

namespace Zarpa.Api.Data.Entities
{
    public class QuestionEntity
    {
        [Key]
        public long ID { get; set; }

        public long TopicID { get; set; }
        public TopicEntity Topic { get; set; }

        [Required]
        public string Text { get; set; }

        // Chart/diagram shown with the statement (Carta de navegación needs these).
        [MaxLength(500)]
        public string? ImageUrl { get; set; }

        // HTML shown in the learning modes; images referenced by URL.
        public string? Explanation { get; set; }

        // Which official exam sitting the question came from, e.g. "PER abril 2023".
        [MaxLength(100)]
        public string? SourceExam { get; set; }

        // Outdated questions (e.g. legislation changes) are deactivated, never deleted,
        // so users' answer history stays valid.
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<AnswerEntity> Answers { get; set; } = [];
    }
}
