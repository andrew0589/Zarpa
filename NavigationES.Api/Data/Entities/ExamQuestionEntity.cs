using System.ComponentModel.DataAnnotations;

namespace NavigationES.Api.Data.Entities
{
    // Question N of an exam paper, stored VERBATIM as printed — deliberately
    // independent of the deduplicated topic bank. Real papers repeat and reword
    // questions across sittings; copying them wholesale keeps every paper exactly
    // as it was and keeps the two content pipelines from interfering. The volume
    // is small (a handful of sittings per year), so duplication is a non-issue.
    public class ExamQuestionEntity
    {
        public long ID { get; set; }

        public long ExamID { get; set; }
        public ExamEntity Exam { get; set; }

        // 1-based position on the exam paper.
        public int Position { get; set; }

        // The topic this position belongs to (from the official blueprint) — the
        // correction needs it for the per-topic error limits.
        public long TopicID { get; set; }
        public TopicEntity Topic { get; set; }

        [Required]
        public string Text { get; set; }

        [Required]
        public string Answer1 { get; set; }

        [Required]
        public string Answer2 { get; set; }

        [Required]
        public string Answer3 { get; set; }

        [Required]
        public string Answer4 { get; set; }

        // 1-based index (1–4) of the correct answer.
        public int CorrectIndex { get; set; }

        // Figure that is part of the statement (balizamiento/lights papers).
        [MaxLength(500)]
        public string? QuestionImageUrl { get; set; }
    }
}
