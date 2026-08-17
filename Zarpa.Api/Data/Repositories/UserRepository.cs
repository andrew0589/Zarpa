using Microsoft.EntityFrameworkCore;

namespace Zarpa.Api.Data.Repositories
{
    public class UserRepository(ZarpaDbContext context) : IUserRepository
    {
        private readonly ZarpaDbContext _context = context;

        public async Task<long?> GetSelectedLicenseIdAsync(long userId)
        {
            return await _context.Users
                .Where(u => u.ID == userId)
                .Select(u => u.SelectedLicenseID)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> SetSelectedLicenseIdAsync(long userId, long licenseId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is null)
                return false;

            user.SelectedLicenseID = licenseId;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
