using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data.Entities;

namespace NavigationES.Api.Data.Repositories
{
    public class QuestionRepository(NavigationESDbContext context) : IQuestionRepository
    {
        private readonly NavigationESDbContext _context = context;

        public async Task<QuestionEntity?> FindByContentHashAsync(string contentHash)
        {
            return await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.ContentHash == contentHash);
        }

        public async Task<List<QuestionEntity>> GetActiveByTopicAsync(long topicId)
        {
            return await _context.Questions
                .Include(q => q.Answers)
                .Where(q => q.TopicID == topicId && q.IsActive)
                .OrderBy(q => q.ID)
                .ToListAsync();
        }

        public async Task<List<QuestionEntity>> GetActiveByIdsAsync(IReadOnlyCollection<long> questionIds)
        {
            return await _context.Questions
                .Include(q => q.Answers)
                .Where(q => questionIds.Contains(q.ID) && q.IsActive)
                .OrderBy(q => q.ID)
                .ToListAsync();
        }

        public async Task<List<long>> GetRandomActiveIdsAsync(IReadOnlyCollection<long> topicIds, int count)
        {
            // NEWID() ordering: fine for a bank of a few thousand rows per license.
            return await _context.Questions
                .Where(q => topicIds.Contains(q.TopicID) && q.IsActive)
                .OrderBy(q => Guid.NewGuid())
                .Select(q => q.ID)
                .Take(count)
                .ToListAsync();
        }

        public async Task<QuestionEntity?> FindActiveWithAnswersAsync(long questionId)
        {
            return await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.ID == questionId && q.IsActive);
        }

        public void Add(QuestionEntity question)
        {
            _context.Questions.Add(question);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
