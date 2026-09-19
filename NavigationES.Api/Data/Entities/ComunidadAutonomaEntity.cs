using System.ComponentModel.DataAnnotations;

namespace NavigationES.Api.Data.Entities
{
    // Reference data: the Spanish autonomous communities that run nautical exams.
    // Seeded in NavigationESDbContext; exams and the user's preference point at it.
    public class ComunidadAutonomaEntity
    {
        [Key]
        public long ID { get; set; }

        [Required, MaxLength(60)]
        public string Name { get; set; }

        // Bookkeeping for the web's Convocatorias tab, all typed in by hand (no
        // community publishes a machine-readable calendar): where the community
        // publishes its convocatorias and exam papers, the next official sitting
        // the administrator is waiting for, and free notes about it (what is still
        // missing, whom to ask, how the papers arrive).
        [MaxLength(500)]
        public string? ConvocatoriaUrl { get; set; }

        public DateOnly? NextExamDate { get; set; }

        [MaxLength(4000)]
        public string? Notes { get; set; }
    }
}
