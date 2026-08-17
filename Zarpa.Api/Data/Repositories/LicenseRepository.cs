using Microsoft.EntityFrameworkCore;
using Zarpa.Api.Data.Entities;

namespace Zarpa.Api.Data.Repositories
{
    public class LicenseRepository(ZarpaDbContext context) : ILicenseRepository
    {
        private readonly ZarpaDbContext _context = context;

        public async Task<List<LicenseEntity>> GetAllAsync()
        {
            return await _context.Licenses
                .OrderBy(l => l.ID)
                .ToListAsync();
        }

        public async Task<bool> IncludesTopicAsync(long licenseId, long topicId)
        {
            return await _context.LicenseTopics
                .AnyAsync(lt => lt.LicenseID == licenseId && lt.TopicID == topicId);
        }
    }
}
