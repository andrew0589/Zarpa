using System.ComponentModel.DataAnnotations;

namespace NavigationES.Api.Data.Entities
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

        // Grants the web's Usuarios tab and the /api/admin/users endpoints. Set by
        // hand in the database (UPDATE Users SET IsAdmin = 1 WHERE Email = ...) —
        // there is deliberately no UI to promote accounts.
        public bool IsAdmin { get; set; }

        // Null for accounts created before the column existed.
        public DateTime? CreatedAt { get; set; } = DateTime.UtcNow;

        // Last authenticated API call, written by UserActivityMiddleware at most once
        // per MinInterval per client. Null = not seen since tracking started. The
        // basis for the inactivity warning and the follow-up cleanup.
        public DateTime? LastActiveAt { get; set; }

        // Which client made that call: "web", "android", "ios", "windows" (from the
        // X-Client header) or "app" for builds older than the header.
        [MaxLength(20)]
        public string? LastActiveClient { get; set; }

        // When an administrator sent the "your account will be deleted" email. The
        // account may be removed a month later if LastActiveAt has not moved since.
        public DateTime? InactivityWarningSentAt { get; set; }
    }
}
