namespace Zarpa.Api.Data.Entities
{
    // What the user answered to one question within a session.
    public class SessionAnswerEntity
    {
        public long ID { get; set; }

        public long SessionID { get; set; }
        public TestSessionEntity Session { get; set; }

        public long QuestionID { get; set; }
        public QuestionEntity Question { get; set; }

        // Null = the question was left unanswered (counts as an error on evaluation).
        public long? ChosenAnswerID { get; set; }
        public AnswerEntity? ChosenAnswer { get; set; }

        // Denormalized so progress/statistics never need to re-join Answers.
        public bool IsCorrect { get; set; }
    }
}
