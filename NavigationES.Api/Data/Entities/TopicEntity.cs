using System.ComponentModel.DataAnnotations;

namespace NavigationES.Api.Data.Entities
{
    // An official syllabus topic (Balizamiento, RIPA, …). Shared across licenses;
    // which topics enter which exam is defined by LicenseTopicEntity.
    public class TopicEntity
    {
        [Key]
        public long ID { get; set; }

        // The official topic number (1–11 for PER).
        public int Number { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; }
    }
}
