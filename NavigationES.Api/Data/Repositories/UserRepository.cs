using Microsoft.EntityFrameworkCore;

namespace NavigationES.Api.Data.Repositories
{
    public class UserRepository(NavigationESDbContext context) : IUserRepository
    {
        private readonly NavigationESDbContext _context = context;

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

        public async Task<long?> GetSelectedComunidadIdAsync(long userId)
        {
            return await _context.Users
                .Where(u => u.ID == userId)
                .Select(u => u.SelectedComunidadAutonomaID)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> SetSelectedComunidadIdAsync(long userId, long comunidadId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user is null)
                return false;

            user.SelectedComunidadAutonomaID = comunidadId;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
