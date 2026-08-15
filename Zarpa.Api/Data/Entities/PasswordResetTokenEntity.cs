namespace Zarpa.Api.Data.Entities
{
    public class PasswordResetTokenEntity
    {
        public long ID { get; set; }
        public long UserID { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public bool IsUsed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public UserEntity? User { get; set; }
    }
}
