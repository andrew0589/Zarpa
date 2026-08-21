using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data.Entities;

namespace NavigationES.Api.Data.Repositories
{
    public class LicenseRepository(NavigationESDbContext context) : ILicenseRepository
    {
        private readonly NavigationESDbContext _context = context;

        public async Task<List<LicenseEntity>> GetAllAsync()
        {
            return await _context.Licenses
                .OrderBy(l => l.ID)
                .ToListAsync();
        }

        public async Task<LicenseEntity?> FindByCodeAsync(string code)
        {
            var trimmed = code.Trim();
            return await _context.Licenses
                .FirstOrDefaultAsync(l => l.Code == trimmed);
        }

        public async Task<bool> IncludesTopicAsync(long licenseId, long topicId)
        {
            return await _context.LicenseTopics
                .AnyAsync(lt => lt.LicenseID == licenseId && lt.TopicID == topicId);
        }
    }
}
