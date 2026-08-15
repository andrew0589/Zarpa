namespace Zarpa.Shared.Enums
{
    // How a test session was taken; drives timing and whether explanations are shown.
    public enum TestMode
    {
        // Timed, exam blueprint, no explanations — like the real exam.
        ExamSimulation = 1,
        // Exam blueprint, untimed, with explanations.
        ExamPractice = 2,
        // Questions from a single topic, untimed, with explanations.
        TopicPractice = 3,
    }
}
