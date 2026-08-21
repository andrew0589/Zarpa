using System.ComponentModel.DataAnnotations;

namespace NavigationES.Api.Data.Entities
{
    // One row per external identity (Google/Apple/Facebook) linked to a user.
    // A user can have several providers plus an optional password on UserEntity.
    public class UserLoginEntity
    {
        [Key]
        public long ID { get; set; }

        public long UserID { get; set; }
        public UserEntity User { get; set; }

        [Required, MaxLength(30)]
        public string Provider { get; set; }

        // Stable user id issued by the provider (Google: the "sub" claim).
        [Required, MaxLength(200)]
        public string ProviderKey { get; set; }

        // Apple only: returned once by the token exchange and kept so the grant can be
        // revoked when the account is deleted, as Guideline 5.1.1(v) requires.
        [MaxLength(500)]
        public string? RefreshToken { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
