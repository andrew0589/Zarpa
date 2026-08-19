using System.ComponentModel.DataAnnotations;

namespace Zarpa.Api.Data.Entities
{
    // Reference data: the Spanish autonomous communities that run nautical exams.
    // Seeded in ZarpaDbContext; exams and the user's preference point at it.
    public class ComunidadAutonomaEntity
    {
        [Key]
        public long ID { get; set; }

        [Required, MaxLength(60)]
        public string Name { get; set; }
    }
}
