using Microsoft.EntityFrameworkCore;
using Zarpa.Api.Data.Entities;

namespace Zarpa.Api.Data
{
    public class ZarpaDbContext : DbContext
    {
        public ZarpaDbContext(DbContextOptions<ZarpaDbContext> options) : base(options)
        {
        }

        public DbSet<UserEntity> Users { get; set; }
        public DbSet<UserLoginEntity> UserLogins { get; set; }
        public DbSet<PasswordResetTokenEntity> PasswordResetTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserEntity>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique();

            modelBuilder.Entity<UserLoginEntity>(ul =>
            {
                ul.HasOne(x => x.User)
                  .WithMany()
                  .HasForeignKey(x => x.UserID)
                  .OnDelete(DeleteBehavior.Cascade);

                ul.HasIndex(x => new { x.Provider, x.ProviderKey })
                  .IsUnique();
            });
        }
    }
}
