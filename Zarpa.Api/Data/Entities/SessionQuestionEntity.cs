namespace Zarpa.Api.Data.Entities
{
    // The planned content of a session, fixed at creation. A full pass plans every
    // active topic question; a retry session plans only the previously failed ones —
    // which is how "resume" and "finished" know what the session is supposed to cover.
    public class SessionQuestionEntity
    {
        public long ID { get; set; }

        public long SessionID { get; set; }
        public TestSessionEntity Session { get; set; }

        public long QuestionID { get; set; }
        public QuestionEntity Question { get; set; }
    }
}
