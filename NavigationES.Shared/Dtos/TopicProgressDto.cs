namespace NavigationES.Shared.Dtos
{
    // One topic with the size of its active question pool and the user's standing on
    // it. A question's state is decided by its LATEST answer: correct → counted in
    // CorrectCount (aciertos), wrong → FailedCount (fallos), never answered → neither.
    public record TopicProgressDto(int Number, string Name, int QuestionCount, int CorrectCount, int FailedCount);

    // Topics are filtered by the user's license, but the global totals span the whole
    // question bank — every question of every topic.
    public record TopicsProgressDto(List<TopicProgressDto> Topics, int TotalQuestions, int TotalCorrectAnswers);
}
