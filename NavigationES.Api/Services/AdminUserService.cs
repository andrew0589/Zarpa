using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;
using NavigationES.Api.Utilities.Email;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Constants.Email;
using NavigationES.Shared.Dtos;
using NavigationES.Shared.Enums;

namespace NavigationES.Api.Services
{
    // Behind the web's Usuarios tab: list every account with its activity, wipe a
    // user's history, warn an inactive user by email, or delete the account.
    public class AdminUserService(NavigationESDbContext context, AuthService authService, IEmailService emailService, IConfiguration configuration)
    {
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 10;

        // Sort orders the tab offers. "inactive" is the cleanup view: never-seen
        // accounts first (NULLs sort first ascending on SQL Server), then the oldest
        // activity.
        public const string OrderRecent = "recent";
        public const string OrderInactive = "inactive";
        public const string OrderNewest = "newest";
        public const string OrderName = "name";

        // Month names spelled out here so the email does not depend on ICU data being
        // present in the container (invariant-globalization images have none).
        private static readonly string[] SpanishMonths =
            ["enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

        private readonly NavigationESDbContext _context = context;
        private readonly AuthService _authService = authService;
        private readonly IEmailService _emailService = emailService;
        private readonly IConfiguration _configuration = configuration;

        public async Task<AdminUserListDto> GetUsersAsync(string? search, string? order, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize <= 0 ? DefaultPageSize : pageSize, 1, MaxPageSize);

            var query = _context.Users.AsNoTracking();

            var term = search?.Trim();
            if (!string.IsNullOrEmpty(term))
                query = query.Where(u => u.Name.Contains(term) || u.Email.Contains(term));

            query = order switch
            {
                OrderInactive => query.OrderBy(u => u.LastActiveAt).ThenBy(u => u.CreatedAt).ThenBy(u => u.ID),
                OrderNewest => query.OrderByDescending(u => u.CreatedAt).ThenByDescending(u => u.ID),
                OrderName => query.OrderBy(u => u.Name).ThenBy(u => u.ID),
                _ => query.OrderByDescending(u => u.LastActiveAt).ThenByDescending(u => u.ID),
            };

            var total = await query.CountAsync();

            // Projected to an anonymous type first: the correlated counts and the
            // provider list translate reliably there, then the record is built in memory.
            var rows = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new
                {
                    u.ID,
                    u.Name,
                    u.Email,
                    u.IsEmailVerified,
                    u.IsAdmin,
                    HasPassword = u.Hash != null,
                    Providers = _context.UserLogins.Where(l => l.UserID == u.ID).Select(l => l.Provider).ToList(),
                    LicenseCode = _context.Licenses.Where(l => l.ID == u.SelectedLicenseID).Select(l => l.Code).FirstOrDefault(),
                    TopicSessions = _context.TestSessions.Count(s => s.UserID == u.ID && s.Mode == TestMode.TopicPractice),
                    ExamSessions = _context.TestSessions.Count(s => s.UserID == u.ID && s.ExamID != null),
                    u.CreatedAt,
                    u.LastActiveAt,
                    u.LastActiveClient,
                    u.InactivityWarningSentAt
                })
                .ToListAsync();

            var items = rows.Select(r => new AdminUserDto(
                r.ID, r.Name, r.Email, r.IsEmailVerified, r.IsAdmin, r.HasPassword, r.Providers, r.LicenseCode,
                r.TopicSessions, r.ExamSessions, r.CreatedAt, r.LastActiveAt, r.LastActiveClient, r.InactivityWarningSentAt))
                .ToList();

            return new AdminUserListDto(items, total, page, pageSize);
        }

        // Deletes every test session of the user — topic practice and exam simulations
        // alike; SessionQuestions, SessionAnswers and ExamSessionAnswers go with them
        // through the database cascades. The account, its sign-in methods and its
        // license/community preferences stay.
        public async Task<ResultDto> ClearHistoryAsync(long userId)
        {
            if (!await _context.Users.AnyAsync(u => u.ID == userId))
                return ResultDto.Failure(ErrorCodes.UserDoesNotExist);

            try
            {
                await _context.TestSessions.Where(s => s.UserID == userId).ExecuteDeleteAsync();
                return ResultDto.Success();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Admin] Clearing history failed for user {userId}: {ex.Message}");
                return ResultDto.Failure(ErrorCodes.UnknownError);
            }
        }

        // The same removal as the user's own "Eliminar cuenta" (Apple grant revoked,
        // database cascades). Administrators cannot be deleted from here — yourself
        // included: clear IsAdmin in the database first, or use Perfil for your own.
        public async Task<ResultDto> DeleteUserAsync(long userId)
        {
            var target = await _context.Users.AsNoTracking()
                .Where(u => u.ID == userId)
                .Select(u => new { u.IsAdmin })
                .FirstOrDefaultAsync();

            if (target is null) return ResultDto.Failure(ErrorCodes.UserDoesNotExist);
            if (target.IsAdmin) return ResultDto.Failure(ErrorCodes.AdminAccountProtectedError);

            return await _authService.DeleteAccountAsync(userId);
        }

        // Emails the user that the account will be deleted in a month unless used, and
        // stamps InactivityWarningSentAt so the tab can show the countdown. Sending is
        // manual on purpose: the administrator decides who is inactive enough.
        public async Task<ResultDto> SendInactivityWarningAsync(long userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userId);
            if (user is null) return ResultDto.Failure(ErrorCodes.UserDoesNotExist);
            if (user.IsAdmin) return ResultDto.Failure(ErrorCodes.AdminAccountProtectedError);

            var now = DateTime.UtcNow;
            var loginUrl = _configuration["AppSettings:FrontendUrl"]?.TrimEnd('/') ?? "https://navigationes.eu";
            var body = EmailTemplates.BuildInactivityWarningBody(user.Name, FormatSpanishDate(now.AddMonths(1)), loginUrl);

            var sent = await _emailService.SendEmailAsync(user.Email, EmailSubjects.InactivityWarning, body, true);
            if (!sent) return ResultDto.Failure(ErrorCodes.EmailSendFailedError);

            try
            {
                user.InactivityWarningSentAt = now;
                await _context.SaveChangesAsync();
                return ResultDto.Success();
            }
            catch (Exception ex)
            {
                // The email is already out; the missing stamp only costs the countdown chip.
                Console.WriteLine($"[Admin] Could not stamp the inactivity warning for user {userId}: {ex.Message}");
                return ResultDto.Failure(ErrorCodes.UnknownError);
            }
        }

        // "12 de octubre de 2026" — the deletion date as the email spells it.
        public static string FormatSpanishDate(DateTime date) =>
            $"{date.Day} de {SpanishMonths[date.Month - 1]} de {date.Year}";
    }
}
