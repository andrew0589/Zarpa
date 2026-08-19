using System.ComponentModel.DataAnnotations;

namespace Zarpa.Api.Data.Entities
{
    public class UserEntity
    {
        [Key]
        public long ID { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        // Canonical form of Email (lowercased; dots/+tags stripped for Gmail),
        // kept in sync by EmailNormalizer at signup. Unique-indexed; all
        // email lookups go through this column instead of Email.
        [Required, MaxLength(100)]
        public string NormalizedEmail { get; set; }

        // Null for accounts created through a social provider (Google/Apple/Facebook) —
        // they have no password; identity is proven by the provider's signed ID token.
        [MaxLength(150)]
        public string? Salt { get; set; }

        [MaxLength(180)]
        public string? Hash { get; set; }

        [MaxLength(6)]
        public string? EmailVerificationCode { get; set; }

        public DateTime? EmailVerificationExpiry { get; set; }

        public bool IsEmailVerified { get; set; } = false;

        // The qualification (PNB/PER/PY/CY) the user prepares for, chosen on the Tests
        // tab. Stored per account so it follows the user across devices and reinstalls.
        public long? SelectedLicenseID { get; set; }

        // The autonomous community whose real exams the user wants to simulate.
        // Null = no preference yet (all communities' exams are offered).
        public long? SelectedComunidadAutonomaID { get; set; }
    }
}
