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
