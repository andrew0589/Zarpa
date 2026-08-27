using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data.Entities;

namespace NavigationES.Api.Data
{
    public class NavigationESDbContext : DbContext
    {
        public NavigationESDbContext(DbContextOptions<NavigationESDbContext> options) : base(options)
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
        public DbSet<SessionQuestionEntity> SessionQuestions { get; set; }
        public DbSet<SessionAnswerEntity> SessionAnswers { get; set; }
        public DbSet<ComunidadAutonomaEntity> ComunidadesAutonomas { get; set; }
        public DbSet<ExamEntity> Exams { get; set; }
        public DbSet<ExamQuestionEntity> ExamQuestions { get; set; }
        public DbSet<ExamSessionAnswerEntity> ExamSessionAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<UserEntity>()
                .HasIndex(u => u.NormalizedEmail)
                .IsUnique();

            modelBuilder.Entity<UserEntity>()
                .HasOne<LicenseEntity>()
                .WithMany()
                .HasForeignKey(u => u.SelectedLicenseID)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserEntity>()
                .HasOne<ComunidadAutonomaEntity>()
                .WithMany()
                .HasForeignKey(u => u.SelectedComunidadAutonomaID)
                .OnDelete(DeleteBehavior.Restrict);

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

                // The duplicate guard: same normalized statement cannot enter twice.
                q.HasIndex(x => x.ContentHash)
                 .IsUnique();
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

                // A real-exam simulation references the paper it replays; the paper
                // cannot be deleted while sessions exist for it.
                s.HasOne(x => x.Exam)
                 .WithMany()
                 .HasForeignKey(x => x.ExamID)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ExamEntity>(e =>
            {
                e.HasOne(x => x.ComunidadAutonoma)
                 .WithMany()
                 .HasForeignKey(x => x.ComunidadAutonomaID)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.License)
                 .WithMany()
                 .HasForeignKey(x => x.LicenseID)
                 .OnDelete(DeleteBehavior.Restrict);

                // Deliberately NOT unique: the import always inserts, duplicates are
                // cleaned up manually/by script afterwards (admin decision).
                e.HasIndex(x => new { x.ComunidadAutonomaID, x.LicenseID, x.Year, x.Month });
            });

            modelBuilder.Entity<ExamQuestionEntity>(eq =>
            {
                eq.HasOne(x => x.Exam)
                  .WithMany()
                  .HasForeignKey(x => x.ExamID)
                  .OnDelete(DeleteBehavior.Cascade);

                // Topics are permanent reference data (same rule as QuestionEntity).
                eq.HasOne(x => x.Topic)
                  .WithMany()
                  .HasForeignKey(x => x.TopicID)
                  .OnDelete(DeleteBehavior.Restrict);

                eq.HasIndex(x => new { x.ExamID, x.Position })
                  .IsUnique();
            });

            modelBuilder.Entity<ExamSessionAnswerEntity>(esa =>
            {
                esa.HasOne(x => x.Session)
                   .WithMany()
                   .HasForeignKey(x => x.SessionID)
                   .OnDelete(DeleteBehavior.Cascade);

                // Answered papers block hard deletes of their questions (and, through
                // the Exams cascade, of the exam itself) — same rule as the bank.
                esa.HasOne(x => x.ExamQuestion)
                   .WithMany()
                   .HasForeignKey(x => x.ExamQuestionID)
                   .OnDelete(DeleteBehavior.Restrict);

                esa.HasIndex(x => new { x.SessionID, x.ExamQuestionID })
                   .IsUnique();
            });

            modelBuilder.Entity<SessionQuestionEntity>(sq =>
            {
                sq.HasOne(x => x.Session)
                  .WithMany()
                  .HasForeignKey(x => x.SessionID)
                  .OnDelete(DeleteBehavior.Cascade);

                sq.HasOne(x => x.Question)
                  .WithMany()
                  .HasForeignKey(x => x.QuestionID)
                  .OnDelete(DeleteBehavior.Restrict);

                sq.HasIndex(x => new { x.SessionID, x.QuestionID })
                  .IsUnique();
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
                // PNB: 27 questions in 45 minutes, at least 17 correct → at most 10 errors.
                new LicenseEntity { ID = 1, Code = "PNB", Name = "Patrón para Navegación Básica", ExamMinutes = 45, MaxTotalErrors = 10 },
                new LicenseEntity { ID = 2, Code = "PER", Name = "Patrón de Embarcaciones de Recreo", ExamMinutes = 90, MaxTotalErrors = 13 },
                // PY: 40 questions in 2 hours. Its topics go deeper than the shared ones, so
                // PY (and CY) get their own topic rows and blueprint later — not configured yet.
                new LicenseEntity { ID = 3, Code = "PY", Name = "Patrón de Yate", ExamMinutes = 120 },
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
                new TopicEntity { ID = 11, Number = 11, Name = "Carta de navegación" },
                // PY and CY examine the same subjects at a deeper level, so they get
                // their own topics rather than reusing 1-11.
                new TopicEntity { ID = 12, Number = 12, Name = "Seguridad en la mar (PY)" },
                new TopicEntity { ID = 13, Number = 13, Name = "Meteorología (PY)" },
                new TopicEntity { ID = 14, Number = 14, Name = "Teoría de navegación (PY)" },
                new TopicEntity { ID = 15, Number = 15, Name = "Navegación carta (PY)" },
                new TopicEntity { ID = 16, Number = 16, Name = "Meteorología (CY)" },
                new TopicEntity { ID = 17, Number = 17, Name = "Inglés (CY)" },
                new TopicEntity { ID = 18, Number = 18, Name = "Teoría de navegación (CY)" },
                new TopicEntity { ID = 19, Number = 19, Name = "Cálculo de navegación (CY)" });

            // PNB: 27 questions across the first six topics, no per-topic error limits.
            modelBuilder.Entity<LicenseTopicEntity>().HasData(
                new LicenseTopicEntity { LicenseID = 1, TopicID = 1, QuestionsInExam = 4 },
                new LicenseTopicEntity { LicenseID = 1, TopicID = 2, QuestionsInExam = 2 },
                new LicenseTopicEntity { LicenseID = 1, TopicID = 3, QuestionsInExam = 4 },
                new LicenseTopicEntity { LicenseID = 1, TopicID = 4, QuestionsInExam = 2 },
                new LicenseTopicEntity { LicenseID = 1, TopicID = 5, QuestionsInExam = 5 },
                new LicenseTopicEntity { LicenseID = 1, TopicID = 6, QuestionsInExam = 10 });

            // The autonomous communities that run their own nautical exam sittings.
            modelBuilder.Entity<ComunidadAutonomaEntity>().HasData(
                new ComunidadAutonomaEntity { ID = 1, Name = "Andalucía" },
                new ComunidadAutonomaEntity { ID = 2, Name = "Cantabria" },
                new ComunidadAutonomaEntity { ID = 3, Name = "Cataluña" },
                new ComunidadAutonomaEntity { ID = 4, Name = "Ciudad Autónoma de Ceuta" },
                new ComunidadAutonomaEntity { ID = 5, Name = "Ciudad Autónoma de Melilla" },
                new ComunidadAutonomaEntity { ID = 6, Name = "Comunidad de Madrid" },
                new ComunidadAutonomaEntity { ID = 7, Name = "Comunidad Valenciana" },
                new ComunidadAutonomaEntity { ID = 8, Name = "Galicia" },
                new ComunidadAutonomaEntity { ID = 9, Name = "Islas Baleares" },
                new ComunidadAutonomaEntity { ID = 10, Name = "Islas Canarias" },
                new ComunidadAutonomaEntity { ID = 11, Name = "País Vasco" },
                new ComunidadAutonomaEntity { ID = 12, Name = "Principado de Asturias" },
                new ComunidadAutonomaEntity { ID = 13, Name = "Región de Murcia" });

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

            // PY and CY: 40 questions each, sat as two separate 20-question modules —
            // genérico (the first two topics) and navegación (the last two). Per-topic
            // error limits are left unset until the official rules are confirmed.
            modelBuilder.Entity<LicenseTopicEntity>().HasData(
                new LicenseTopicEntity { LicenseID = 3, TopicID = 12, QuestionsInExam = 10 },
                new LicenseTopicEntity { LicenseID = 3, TopicID = 13, QuestionsInExam = 10 },
                new LicenseTopicEntity { LicenseID = 3, TopicID = 14, QuestionsInExam = 10 },
                new LicenseTopicEntity { LicenseID = 3, TopicID = 15, QuestionsInExam = 10 },
                new LicenseTopicEntity { LicenseID = 4, TopicID = 16, QuestionsInExam = 10 },
                new LicenseTopicEntity { LicenseID = 4, TopicID = 17, QuestionsInExam = 10 },
                new LicenseTopicEntity { LicenseID = 4, TopicID = 18, QuestionsInExam = 10 },
                new LicenseTopicEntity { LicenseID = 4, TopicID = 19, QuestionsInExam = 10 });
        }
    }
}
