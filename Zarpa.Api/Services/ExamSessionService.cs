using Zarpa.Api.Data.Entities;
using Zarpa.Api.Data.Repositories;
using Zarpa.Shared.Dtos;
using Zarpa.Shared.Enums;

namespace Zarpa.Api.Services
{
    // The real-exam simulation: timed like the official sitting, answers can be
    // changed until the finish, nothing is corrected until the end. Grading follows
    // the official blueprint: unanswered counts as an error, the license's
    // MaxTotalErrors caps the total, and topics with their own MaxErrors (e.g. PER:
    // Balizamiento 2, RIPA 5, Carta 2) must each stay within their limit.
    public class ExamSessionService(IExamSessionRepository repository)
    {
        // The clock keeps accepting answers briefly past the limit — network latency
        // must not eat a legitimate last-second answer.
        private static readonly TimeSpan SubmitGrace = TimeSpan.FromSeconds(60);

        private readonly IExamSessionRepository _repository = repository;

        public async Task<StartExamSessionDto?> StartAsync(long userId, long examId)
        {
            var exam = await _repository.FindExamWithLicenseAsync(examId);
            if (exam is null)
                return null;

            var questions = await _repository.GetExamQuestionsAsync(examId);
            if (questions.Count == 0)
                return null;

            var session = await _repository.FindOpenSessionAsync(userId, examId);

            // A running, non-expired attempt is resumed; anything else (an expired
            // abandoned attempt, or previous finished results) is DELETED before the
            // fresh start — by design only the latest attempt of a paper is kept.
            if (session is not null && IsExpired(session, exam.License.ExamMinutes, grace: TimeSpan.Zero))
                session = null;

            if (session is null)
            {
                await _repository.DeleteSessionsForExamAsync(userId, examId);

                session = new TestSessionEntity
                {
                    UserID = userId,
                    LicenseID = exam.LicenseID,
                    Mode = TestMode.ExamSimulation,
                    ExamID = exam.ID,
                };
                _repository.AddSession(session);
                await _repository.SaveChangesAsync();
            }

            var chosenByQuestion = (await _repository.GetAnswersAsync(session.ID))
                .ToDictionary(a => a.ExamQuestionID, a => (int?)a.ChosenIndex);

            int? remainingSeconds = exam.License.ExamMinutes is int minutes
                ? Math.Max(0, (int)(minutes * 60 - (DateTime.UtcNow - session.StartedAt).TotalSeconds))
                : null;

            return new StartExamSessionDto(
                session.ID,
                questions.Count,
                remainingSeconds,
                [.. questions.Select(q => new ExamSessionQuestionDto(
                    q.ID,
                    q.Position,
                    q.Text,
                    [q.Answer1, q.Answer2, q.Answer3, q.Answer4],
                    q.QuestionImageUrl,
                    chosenByQuestion.GetValueOrDefault(q.ID)))]);
        }

        public async Task<bool> SubmitAnswerAsync(long userId, long sessionId, SubmitExamAnswerRequestDto request)
        {
            if (request.ChosenIndex is < 1 or > 4)
                return false;

            var session = await _repository.FindSessionAsync(sessionId, userId);
            if (session is null || session.ExamID is not long examId || session.FinishedAt is not null)
                return false;

            var exam = await _repository.FindExamWithLicenseAsync(examId);
            if (exam is null || IsExpired(session, exam.License.ExamMinutes, SubmitGrace))
                return false;

            var question = await _repository.FindExamQuestionAsync(request.ExamQuestionId);
            if (question is null || question.ExamID != examId)
                return false;

            var existing = await _repository.FindAnswerAsync(sessionId, question.ID);
            if (existing is null)
            {
                _repository.AddAnswer(new ExamSessionAnswerEntity
                {
                    SessionID = session.ID,
                    ExamQuestionID = question.ID,
                    ChosenIndex = request.ChosenIndex,
                    IsCorrect = request.ChosenIndex == question.CorrectIndex,
                });
            }
            else
            {
                // Changing your mind is allowed until the paper is handed in.
                existing.ChosenIndex = request.ChosenIndex;
                existing.IsCorrect = request.ChosenIndex == question.CorrectIndex;
            }

            await _repository.SaveChangesAsync();
            return true;
        }

        public async Task<ExamSessionResultDto?> FinishAsync(long userId, long sessionId)
        {
            var session = await _repository.FindSessionAsync(sessionId, userId);
            if (session is null || session.ExamID is not long examId)
                return null;

            var exam = await _repository.FindExamWithLicenseAsync(examId);
            if (exam is null)
                return null;

            var questions = await _repository.GetExamQuestionsAsync(examId);

            // Finishing twice returns the stored verdict instead of failing.
            if (session.FinishedAt is null)
                await GradeAndCloseAsync(session, exam, questions);

            return await BuildResultAsync(session, exam, questions);
        }

        // The user walked out mid-exam and confirmed it: the attempt is discarded
        // entirely (answers cascade) instead of being graded.
        public async Task<bool> AbandonAsync(long userId, long sessionId)
        {
            var session = await _repository.FindSessionAsync(sessionId, userId);
            if (session is null || session.ExamID is null || session.FinishedAt is not null)
                return false;

            await _repository.DeleteSessionAsync(session);
            return true;
        }

        // Review of an already-graded attempt (page refreshes must not lose the report).
        public async Task<ExamSessionResultDto?> GetResultAsync(long userId, long sessionId)
        {
            var session = await _repository.FindSessionAsync(sessionId, userId);
            if (session is null || session.ExamID is not long examId || session.FinishedAt is null)
                return null;

            var exam = await _repository.FindExamWithLicenseAsync(examId);
            if (exam is null)
                return null;

            return await BuildResultAsync(session, exam, await _repository.GetExamQuestionsAsync(examId));
        }

        private static bool IsExpired(TestSessionEntity session, int? examMinutes, TimeSpan grace)
        {
            if (examMinutes is not int minutes)
                return false;

            return DateTime.UtcNow - session.StartedAt > TimeSpan.FromMinutes(minutes) + grace;
        }

        private async Task GradeAndCloseAsync(TestSessionEntity session, ExamEntity exam, List<ExamQuestionEntity> questions)
        {
            var answers = await _repository.GetAnswersAsync(session.ID);
            session.Passed = ComputePassed(exam, questions, answers,
                await _repository.GetTopicErrorLimitsAsync(exam.LicenseID));
            session.FinishedAt = DateTime.UtcNow;
            await _repository.SaveChangesAsync();
        }

        private static bool ComputePassed(
            ExamEntity exam,
            List<ExamQuestionEntity> questions,
            List<ExamSessionAnswerEntity> answers,
            Dictionary<long, int?> topicLimits)
        {
            var correctByQuestion = answers.ToDictionary(a => a.ExamQuestionID, a => a.IsCorrect);

            var totalErrors = questions.Count(q => !correctByQuestion.GetValueOrDefault(q.ID));
            if (totalErrors > (exam.License.MaxTotalErrors ?? int.MaxValue))
                return false;

            foreach (var topicGroup in questions.GroupBy(q => q.TopicID))
            {
                if (topicLimits.GetValueOrDefault(topicGroup.Key) is not int limit)
                    continue;

                var topicErrors = topicGroup.Count(q => !correctByQuestion.GetValueOrDefault(q.ID));
                if (topicErrors > limit)
                    return false;
            }

            return true;
        }

        private async Task<ExamSessionResultDto> BuildResultAsync(
            TestSessionEntity session, ExamEntity exam, List<ExamQuestionEntity> questions)
        {
            var answers = await _repository.GetAnswersAsync(session.ID);
            var topicLimits = await _repository.GetTopicErrorLimitsAsync(exam.LicenseID);
            var chosenByQuestion = answers.ToDictionary(a => a.ExamQuestionID, a => a.ChosenIndex);

            var correct = questions.Count(q =>
                chosenByQuestion.TryGetValue(q.ID, out var chosen) && chosen == q.CorrectIndex);
            var answered = questions.Count(q => chosenByQuestion.ContainsKey(q.ID));
            var wrong = answered - correct;
            var unanswered = questions.Count - answered;

            var topics = questions
                .GroupBy(q => q.Topic)
                .OrderBy(g => g.Key.Number)
                .Select(g =>
                {
                    var errors = g.Count(q =>
                        !chosenByQuestion.TryGetValue(q.ID, out var chosen) || chosen != q.CorrectIndex);
                    var limit = topicLimits.GetValueOrDefault(g.Key.ID);
                    return new ExamTopicResultDto(
                        g.Key.Number, g.Key.Name, g.Count(), errors, limit,
                        limit is not int max || errors <= max);
                })
                .ToList();

            return new ExamSessionResultDto(
                session.ID,
                session.Passed == true,
                questions.Count,
                correct,
                wrong,
                unanswered,
                questions.Count - correct,
                exam.License.MaxTotalErrors,
                topics,
                [.. questions.Select(q => new ExamResultQuestionDto(
                    q.ID,
                    q.Position,
                    q.Topic.Number,
                    q.Topic.Name,
                    q.Text,
                    [q.Answer1, q.Answer2, q.Answer3, q.Answer4],
                    q.CorrectIndex,
                    chosenByQuestion.TryGetValue(q.ID, out var chosen) ? chosen : null))]);
        }
    }
}
