using System.ComponentModel.DataAnnotations;

namespace Zarpa.Api.Data.Entities
{
    // One of the four options of a question. Exactly one per question has
    // IsCorrect = true, enforced by a filtered unique index in ZarpaDbContext.
    public class AnswerEntity
    {
        [Key]
        public long ID { get; set; }

        public long QuestionID { get; set; }
        public QuestionEntity Question { get; set; }

        [Required, MaxLength(1000)]
        public string Text { get; set; }

        public bool IsCorrect { get; set; }

        // Display position (1–4), matching the official exam's a/b/c/d order.
        public int Order { get; set; }
    }
}
