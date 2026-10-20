using NavigationES.Api.Data.Entities;
using NavigationES.Api.Data.Repositories;
using NavigationES.Shared.Dtos;
using NavigationES.Shared.Enums;

namespace NavigationES.Api.Services
{
    // Topic-practice sessions: starting resumes the user's unfinished session for that
    // topic and license (question 11 of 20 continues at 11), each answer is persisted
    // as it happens, and the session closes itself when the last question is answered.
    public class PracticeSessionService(
        ITopicRepository topicRepository,
        IQuestionRepository questionRepository,
        ILicenseRepository licenseRepository,
        ISessionRepository sessionRepository)
    {
        private readonly ITopicRepository _topicRepository = topicRepository;
        private readonly IQuestionRepository _questionRepository = questionRepository;
        private readonly ILicenseRepository _licenseRepository = licenseRepository;
        private readonly ISessionRepository _sessionRepository = sessionRepository;

        // "Mini test": questions per session.
        public const int QuickTestSize = 10;

        // "Repasar mis fallos": at most this many per session, so a long list is
        // worked off in sittings; the next start plans the rest.
        public const int ReviewBatchSize = 25;

        public async Task<PracticeSessionDto?> StartTopicPracticeAsync(long userId, StartTopicPracticeRequestDto request)
        {
            var topics = await _topicRepository.GetByNumberAsync();
            if (!topics.TryGetValue(request.TopicNumber, out var topic))
                return null;

            // The topic must belong to the chosen license's blueprint — a PNB user
            // cannot open a session on Carta de navegación.
            if (!await _licenseRepository.IncludesTopicAsync(request.LicenseId, topic.ID))
                return null;

            var questions = await _questionRepository.GetActiveByTopicAsync(topic.ID);
            if (questions.Count == 0)
                return null;

            var session = await _sessionRepository.FindOpenTopicSessionAsync(userId, topic.ID, request.LicenseId);
            var plannedIds = new HashSet<long>();
            var answeredIds = new HashSet<long>();

            if (session is not null)
            {
                plannedIds = await _sessionRepository.GetPlannedQuestionIdsAsync(session.ID);
                answeredIds = await _sessionRepository.GetAnsweredQuestionIdsAsync(session.ID);

                // Fully answered but never closed (e.g. the app died on the last answer):
                // close it now and start fresh.
                if (plannedIds.All(answeredIds.Contains))
                {
                    session.FinishedAt = DateTime.UtcNow;
                    session = null;
                }
            }

            if (session is null)
            {
                // Questions whose latest answer was wrong come first: a new session
                // covers only those; with nothing left to fix, it is a full pass.
                var failedIds = await _sessionRepository.GetLatestFailedQuestionIdsAsync(userId, topic.ID);
                plannedIds = failedIds.Count > 0
                    ? [.. failedIds]
                    : [.. questions.Select(q => q.ID)];
                answeredIds = [];

                session = new TestSessionEntity
                {
                    UserID = userId,
                    LicenseID = request.LicenseId,
                    Mode = TestMode.TopicPractice,
                    TopicID = topic.ID,
                };
                _sessionRepository.AddSession(session);
                _sessionRepository.AddPlannedQuestions(session, plannedIds);
            }

            await _sessionRepository.SaveChangesAsync();

            var remaining = questions
                .Where(q => plannedIds.Contains(q.ID) && !answeredIds.Contains(q.ID))
                .Select(q => new SessionQuestionDto(
                    q.ID,
                    q.Text,
                    [.. q.Answers.OrderBy(a => a.Order).Select(a => new SessionAnswerOptionDto(a.ID, a.Text))]))
                .ToList();

            return new PracticeSessionDto(session.ID, plannedIds.Count, answeredIds.Count, remaining);
        }

        // "Repasar mis fallos" (Review) and "Mini test" (Quick): sessions over every topic
        // of the license. An unfinished one is resumed, like a topic session. Review
        // plans the questions whose latest answer was wrong (null when there are none);
        // Quick plans QuickTestSize random questions. Answers count towards topic
        // progress like any other: progress reads each question's latest answer.
        public async Task<PracticeSessionDto?> StartLicensePracticeAsync(long userId, long licenseId, TestMode mode)
        {
            if (mode is not (TestMode.Review or TestMode.Quick))
                return null;

            var topicIds = await _licenseRepository.GetTopicIdsAsync(licenseId);
            if (topicIds.Count == 0)
                return null;

            var session = await _sessionRepository.FindOpenLicenseSessionAsync(userId, licenseId, mode);
            var plannedIds = new HashSet<long>();
            var answeredIds = new HashSet<long>();

            if (session is not null)
            {
                plannedIds = await _sessionRepository.GetPlannedQuestionIdsAsync(session.ID);
                answeredIds = await _sessionRepository.GetAnsweredQuestionIdsAsync(session.ID);
                if (plannedIds.All(answeredIds.Contains))
                {
                    session.FinishedAt = DateTime.UtcNow;
                    session = null;
                }
            }

            if (session is null)
            {
                List<long> ids = mode == TestMode.Review
                    ? [.. (await _sessionRepository.GetLatestFailedQuestionIdsAsync(userId, topicIds)).Order().Take(ReviewBatchSize)]
                    : await _questionRepository.GetRandomActiveIdsAsync(topicIds, QuickTestSize);
                if (ids.Count == 0)
                {
                    await _sessionRepository.SaveChangesAsync();
                    return null;
                }

                plannedIds = [.. ids];
                answeredIds = [];
                session = new TestSessionEntity { UserID = userId, LicenseID = licenseId, Mode = mode };
                _sessionRepository.AddSession(session);
                _sessionRepository.AddPlannedQuestions(session, plannedIds);
            }

            await _sessionRepository.SaveChangesAsync();

            var remainingIds = plannedIds.Where(id => !answeredIds.Contains(id)).ToList();
            var questions = await _questionRepository.GetActiveByIdsAsync(remainingIds);
            var remaining = questions
                .Select(q => new SessionQuestionDto(
                    q.ID,
                    q.Text,
                    [.. q.Answers.OrderBy(a => a.Order).Select(a => new SessionAnswerOptionDto(a.ID, a.Text))]))
                .ToList();

            return new PracticeSessionDto(session.ID, plannedIds.Count, answeredIds.Count, remaining);
        }

        // Deletes the user's whole TopicPractice history for the topic, so the next
        // start plans the full question set again instead of the retry subset.
        public async Task<bool> ResetTopicPracticeAsync(long userId, ResetTopicPracticeRequestDto request)
        {
            var topics = await _topicRepository.GetByNumberAsync();
            if (!topics.TryGetValue(request.TopicNumber, out var topic))
                return false;

            // Same guard as starting: the topic must be part of the chosen license.
            if (!await _licenseRepository.IncludesTopicAsync(request.LicenseId, topic.ID))
                return false;

            await _sessionRepository.DeleteTopicSessionsAsync(userId, topic.ID);
            return true;
        }

        public async Task<SubmitAnswerResultDto?> SubmitAnswerAsync(long userId, long sessionId, SubmitAnswerRequestDto request)
        {
            var session = await _sessionRepository.FindByIdAsync(sessionId, userId);
            if (session is null || session.Mode is not (TestMode.TopicPractice or TestMode.Review or TestMode.Quick))
                return null;

            var question = await _questionRepository.FindActiveWithAnswersAsync(request.QuestionId);
            if (question is null)
                return null;

            // Only questions the session was created to cover can be answered in it.
            var plannedIds = await _sessionRepository.GetPlannedQuestionIdsAsync(session.ID);
            if (!plannedIds.Contains(question.ID))
                return null;

            var correct = question.Answers.First(a => a.IsCorrect);

            // Re-submitting an already answered question (retry after a lost response)
            // returns the recorded outcome instead of failing on the unique index.
            var existing = await _sessionRepository.FindAnswerAsync(sessionId, request.QuestionId);
            if (existing is not null)
                return new SubmitAnswerResultDto(existing.IsCorrect, correct.ID, question.Explanation, question.ExplanationImageUrl, session.FinishedAt is not null);

            if (session.FinishedAt is not null)
                return null;

            var chosen = question.Answers.FirstOrDefault(a => a.ID == request.AnswerId);
            if (chosen is null)
                return null;

            _sessionRepository.AddAnswer(new SessionAnswerEntity
            {
                SessionID = session.ID,
                QuestionID = question.ID,
                ChosenAnswerID = chosen.ID,
                IsCorrect = chosen.IsCorrect,
            });

            var answeredSoFar = await _sessionRepository.CountAnswersAsync(session.ID) + 1;
            var finished = answeredSoFar >= plannedIds.Count;
            if (finished)
                session.FinishedAt = DateTime.UtcNow;

            await _sessionRepository.SaveChangesAsync();

            return new SubmitAnswerResultDto(chosen.IsCorrect, correct.ID, question.Explanation, question.ExplanationImageUrl, finished);
        }
    }
}
