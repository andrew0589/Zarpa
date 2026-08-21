using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data.Entities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Data.Repositories
{
    public class TopicRepository(NavigationESDbContext context) : ITopicRepository
    {
        private readonly NavigationESDbContext _context = context;

        // Three single-purpose queries composed in memory — with 11 topics that is
        // cheaper to read and just as cheap to run as one correlated query.
        public async Task<TopicsProgressDto> GetTopicsWithProgressAsync(long userId, long licenseId)
        {
            // Only the topics that are part of this license's exam blueprint — a PNB
            // user sees 6 topics, a PER user 11.
            var topics = await _context.LicenseTopics
                .Where(lt => lt.LicenseID == licenseId)
                .Select(lt => lt.Topic)
                .OrderBy(t => t.Number)
                .ToListAsync();

            // Active questions per topic.
            var questionCounts = await _context.Questions
                .Where(q => q.IsActive)
                .GroupBy(q => q.TopicID)
                .Select(g => new { TopicID = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.TopicID, x => x.Count);

            // Each question's state is its LATEST answer by this user (answer IDs are
            // monotonically increasing, so max ID per question = most recent).
            var latestAnswerIds = _context.SessionAnswers
                .Where(sa => sa.Session.UserID == userId && sa.Question.IsActive)
                .GroupBy(sa => sa.QuestionID)
                .Select(g => g.Max(sa => sa.ID));

            var latestStates = await _context.SessionAnswers
                .Where(sa => latestAnswerIds.Contains(sa.ID))
                .Select(sa => new { sa.Question.TopicID, sa.IsCorrect })
                .ToListAsync();

            var correctCounts = latestStates
                .Where(s => s.IsCorrect)
                .GroupBy(s => s.TopicID)
                .ToDictionary(g => g.Key, g => g.Count());

            var failedCounts = latestStates
                .Where(s => !s.IsCorrect)
                .GroupBy(s => s.TopicID)
                .ToDictionary(g => g.Key, g => g.Count());

            // The count dictionaries span ALL topics, so their sums are the global
            // totals even though the topic list below is license-filtered.
            return new TopicsProgressDto(
                [.. topics.Select(t => new TopicProgressDto(
                    t.Number,
                    t.Name,
                    questionCounts.GetValueOrDefault(t.ID),
                    correctCounts.GetValueOrDefault(t.ID),
                    failedCounts.GetValueOrDefault(t.ID)))],
                questionCounts.Values.Sum(),
                correctCounts.Values.Sum());
        }

        public async Task<Dictionary<int, TopicEntity>> GetByNumberAsync()
        {
            return await _context.Topics.ToDictionaryAsync(t => t.Number);
        }
    }
}
