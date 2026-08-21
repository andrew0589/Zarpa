using NavigationES.Shared.Enums;

namespace NavigationES.Api.Data.Entities
{
    // One taken (or in-progress) test of any mode. Per-topic progress and pass/fail
    // statistics are derived from this plus SessionAnswerEntity — no separate
    // progress table to keep in sync.
    public class TestSessionEntity
    {
        public long ID { get; set; }

        public long UserID { get; set; }
        public UserEntity User { get; set; }

        public long LicenseID { get; set; }
        public LicenseEntity License { get; set; }

        public TestMode Mode { get; set; }

        // Only for TestMode.TopicPractice: the practiced topic.
        public long? TopicID { get; set; }
        public TopicEntity? Topic { get; set; }

        // Only for real-exam simulations: the official paper being replayed.
        public long? ExamID { get; set; }
        public ExamEntity? Exam { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        // Null while the session is in progress or was abandoned.
        public DateTime? FinishedAt { get; set; }

        // Evaluated against the license's blueprint; null for practice modes.
        public bool? Passed { get; set; }
    }
}
