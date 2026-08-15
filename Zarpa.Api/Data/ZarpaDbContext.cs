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
        public DbSet<LicenseEntity> Licenses { get; set; }
        public DbSet<TopicEntity> Topics { get; set; }
        public DbSet<LicenseTopicEntity> LicenseTopics { get; set; }
        public DbSet<QuestionEntity> Questions { get; set; }
        public DbSet<AnswerEntity> Answers { get; set; }
        public DbSet<TestSessionEntity> TestSessions { get; set; }
        public DbSet<SessionAnswerEntity> SessionAnswers { get; set; }

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

            modelBuilder.Entity<TopicEntity>()
                .HasIndex(t => t.Number)
                .IsUnique();

            modelBuilder.Entity<LicenseTopicEntity>(lt =>
            {
                lt.HasKey(x => new { x.LicenseID, x.TopicID });

                lt.HasOne(x => x.License)
                  .WithMany(l => l.Topics)
                  .HasForeignKey(x => x.LicenseID)
                  .OnDelete(DeleteBehavior.Cascade);

                lt.HasOne(x => x.Topic)
                  .WithMany()
                  .HasForeignKey(x => x.TopicID)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<QuestionEntity>(q =>
            {
                // Topics are permanent reference data; a topic with questions cannot go away.
                q.HasOne(x => x.Topic)
                 .WithMany()
                 .HasForeignKey(x => x.TopicID)
                 .OnDelete(DeleteBehavior.Restrict);

                q.HasIndex(x => x.TopicID);
            });

            modelBuilder.Entity<AnswerEntity>(a =>
            {
                a.HasOne(x => x.Question)
                 .WithMany(qu => qu.Answers)
                 .HasForeignKey(x => x.QuestionID)
                 .OnDelete(DeleteBehavior.Cascade);

                // At most one correct answer per question, enforced by the database.
                a.HasIndex(x => x.QuestionID)
                 .IsUnique()
                 .HasFilter("[IsCorrect] = 1");
            });

            modelBuilder.Entity<TestSessionEntity>(s =>
            {
                s.HasOne(x => x.User)
                 .WithMany()
                 .HasForeignKey(x => x.UserID)
                 .OnDelete(DeleteBehavior.Cascade);

                s.HasOne(x => x.License)
                 .WithMany()
                 .HasForeignKey(x => x.LicenseID)
                 .OnDelete(DeleteBehavior.Restrict);

                s.HasOne(x => x.Topic)
                 .WithMany()
                 .HasForeignKey(x => x.TopicID)
                 .OnDelete(DeleteBehavior.Restrict);

                s.HasIndex(x => x.UserID);
            });

            modelBuilder.Entity<SessionAnswerEntity>(sa =>
            {
                sa.HasOne(x => x.Session)
                  .WithMany()
                  .HasForeignKey(x => x.SessionID)
                  .OnDelete(DeleteBehavior.Cascade);

                // Answered questions block hard deletes — retire questions via IsActive instead.
                sa.HasOne(x => x.Question)
                  .WithMany()
                  .HasForeignKey(x => x.QuestionID)
                  .OnDelete(DeleteBehavior.Restrict);

                sa.HasOne(x => x.ChosenAnswer)
                  .WithMany()
                  .HasForeignKey(x => x.ChosenAnswerID)
                  .OnDelete(DeleteBehavior.Restrict);

                sa.HasIndex(x => new { x.SessionID, x.QuestionID })
                  .IsUnique();
            });

            SeedReferenceData(modelBuilder);
        }

        // Static reference data: the four licenses, the official topics and the PER
        // exam blueprint (from the official exam structure). Questions are imported
        // separately from past exam PDFs.
        private static void SeedReferenceData(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<LicenseEntity>().HasData(
                new LicenseEntity { ID = 1, Code = "PNB", Name = "Patrón para Navegación Básica", ExamMinutes = 45 },
                new LicenseEntity { ID = 2, Code = "PER", Name = "Patrón de Embarcaciones de Recreo", ExamMinutes = 90, MaxTotalErrors = 13 },
                // TODO: fill in the exam rules (minutes, errors, blueprint) for PY and CY,
                // and the PNB blueprint, once their official structures are added.
                new LicenseEntity { ID = 3, Code = "PY", Name = "Patrón de Yate" },
                new LicenseEntity { ID = 4, Code = "CY", Name = "Capitán de Yate" });

            modelBuilder.Entity<TopicEntity>().HasData(
                new TopicEntity { ID = 1, Number = 1, Name = "Nomenclatura náutica" },
                new TopicEntity { ID = 2, Number = 2, Name = "Elementos de amarre y fondeo" },
                new TopicEntity { ID = 3, Number = 3, Name = "Seguridad" },
                new TopicEntity { ID = 4, Number = 4, Name = "Legislación" },
                new TopicEntity { ID = 5, Number = 5, Name = "Balizamiento" },
                new TopicEntity { ID = 6, Number = 6, Name = "Reglamento (RIPA)" },
                new TopicEntity { ID = 7, Number = 7, Name = "Maniobra y navegación" },
                new TopicEntity { ID = 8, Number = 8, Name = "Emergencias en la mar" },
                new TopicEntity { ID = 9, Number = 9, Name = "Meteorología" },
                new TopicEntity { ID = 10, Number = 10, Name = "Teoría de la navegación" },
                new TopicEntity { ID = 11, Number = 11, Name = "Carta de navegación" });

            // PER: 45 questions, max 13 total errors; Balizamiento, RIPA and Carta de
            // navegación additionally have their own per-topic error limits.
            modelBuilder.Entity<LicenseTopicEntity>().HasData(
                new LicenseTopicEntity { LicenseID = 2, TopicID = 1, QuestionsInExam = 4 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 2, QuestionsInExam = 2 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 3, QuestionsInExam = 4 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 4, QuestionsInExam = 2 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 5, QuestionsInExam = 5, MaxErrors = 2 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 6, QuestionsInExam = 10, MaxErrors = 5 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 7, QuestionsInExam = 2 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 8, QuestionsInExam = 3 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 9, QuestionsInExam = 4 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 10, QuestionsInExam = 5 },
                new LicenseTopicEntity { LicenseID = 2, TopicID = 11, QuestionsInExam = 4, MaxErrors = 2 });
        }
    }
}
