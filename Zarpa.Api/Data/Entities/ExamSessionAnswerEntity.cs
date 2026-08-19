namespace Zarpa.Api.Data.Entities
{
    // The user's answer to one exam-paper question within a simulation session.
    // Separate from SessionAnswerEntity by design: exam questions live in their own
    // verbatim tables, so their answers do too. Answers can be CHANGED while the
    // session runs (real exams allow it) — one row per question, updated in place.
    public class ExamSessionAnswerEntity
    {
        public long ID { get; set; }

        public long SessionID { get; set; }
        public TestSessionEntity Session { get; set; }

        public long ExamQuestionID { get; set; }
        public ExamQuestionEntity ExamQuestion { get; set; }

        // 1-based index (1–4) of the chosen answer.
        public int ChosenIndex { get; set; }

        public bool IsCorrect { get; set; }
    }
}
