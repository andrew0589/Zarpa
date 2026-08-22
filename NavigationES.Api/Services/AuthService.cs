using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;
using NavigationES.Api.Data.Entities;
using NavigationES.Api.Utilities.Email;
using NavigationES.Api.Utilities.Verification;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Constants.Email;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Services
{
    public class AuthService(NavigationESDbContext context, TokenService tokenService, PasswordService passwordService, IConfiguration configuration, IEmailService emailService, AppleAuthService appleAuthService)
    {
        private readonly NavigationESDbContext _context = context;
        private readonly TokenService _tokenService = tokenService;
        private readonly PasswordService _passwordService = passwordService;
        private readonly IConfiguration _configuration = configuration;
        private readonly IEmailService _emailService = emailService;
        private readonly AppleAuthService _appleAuthService = appleAuthService;

        public async Task<ResultWithDataDto<AuthResponseDto>> SignupAsync(SignupRequestDto dto)
        {
            var normalizedEmail = EmailNormalizer.Normalize(dto.Email);

            var userAlreadyInDB = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
            if (userAlreadyInDB != null)
            {
                if (userAlreadyInDB.IsEmailVerified == true)
                {
                    return ResultWithDataDto<AuthResponseDto>.Failure(ErrorCodes.EmailAlreadyExistsError);
                }
                else
                {
                    // Unverified leftover from an abandoned signup — replace it.
                    _context.Users.Remove(userAlreadyInDB);
                }
            }

            var user = new UserEntity
            {
                Email = dto.Email.Trim(),
                NormalizedEmail = normalizedEmail,
                Name = dto.Name
            };

            (user.Salt, user.Hash) = _passwordService.GenerateSaltAndHash(dto.Password);

            var (code, expiry) = VerificationCodeHelper.Generate();
            user.EmailVerificationCode = code;
            user.EmailVerificationExpiry = expiry;

            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();

                var subject = EmailSubjects.EmailVerification;
                var body = EmailTemplates.BuildVerificationBody(user.Name, user.EmailVerificationCode, 15);

                await _emailService.SendEmailAsync(user.Email, subject, body, true);

                return GenerateAuthResponse(user);
            }
            catch (DbUpdateException)
            {
                // Unique index on NormalizedEmail: two concurrent signups raced past the check above.
                return ResultWithDataDto<AuthResponseDto>.Failure(ErrorCodes.EmailAlreadyExistsError);
            }
            catch (Exception)
            {
                return ResultWithDataDto<AuthResponseDto>.Failure(ErrorCodes.UnknownError);
            }
        }

        public async Task<ResultWithDataDto<AuthResponseDto>> SigninAsync(SigninRequestDto dto)
        {
            var normalizedEmail = EmailNormalizer.Normalize(dto.Email);
            var dbUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (dbUser is null) return ResultWithDataDto<AuthResponseDto>.Failure(ErrorCodes.UserDoesNotExist);

            if (dbUser.IsEmailVerified is false) return ResultWithDataDto<AuthResponseDto>.Failure(ErrorCodes.ValidateYourEmail);

            // Social-only accounts (Google/Apple/Facebook) have no password to compare against.
            if (dbUser.Salt is null || dbUser.Hash is null)
                return ResultWithDataDto<AuthResponseDto>.Failure(ErrorCodes.UseSocialSigninError);

            if (!_passwordService.AreEqual(dto.Password, dbUser.Salt, dbUser.Hash))
                return ResultWithDataDto<AuthResponseDto>.Failure(ErrorCodes.IncorrectPasswordError);

            return GenerateAuthResponse(dbUser);
        }

        // Signin/signup via an external provider whose ID token was already validated.
        // Order: existing link by (provider, key) → link to existing account by email → create account.
        public async Task<ResultWithDataDto<AuthResponseDto>> SocialSigninAsync(string provider, string providerKey, string email, string? name, bool emailVerified, string? refreshToken = null)
        {
            if (string.IsNullOrWhiteSpace(email))
                return ResultWithDataDto<AuthResponseDto>.Failure(provider switch
                {
                    AppleAuthService.ProviderName => ErrorCodes.AppleAuthFailedError,
                    _ => ErrorCodes.GoogleAuthFailedError
                });

            try
            {
                var login = await _context.UserLogins
                    .Include(l => l.User)
                    .FirstOrDefaultAsync(l => l.Provider == provider && l.ProviderKey == providerKey);

                var user = login?.User;
                var isNewUser = false;

                if (user is null)
                {
                    var normalizedEmail = EmailNormalizer.Normalize(email);
                    user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

                    if (user is null)
                    {
                        var displayName = string.IsNullOrWhiteSpace(name) ? email.Split('@')[0] : name.Trim();
                        if (displayName.Length > 50) displayName = displayName[..50];

                        // No password → Salt/Hash stay null; the provider's token is the credential.
                        user = new UserEntity
                        {
                            Name = displayName,
                            Email = email.Trim(),
                            NormalizedEmail = normalizedEmail,
                            IsEmailVerified = emailVerified
                        };

                        await _context.Users.AddAsync(user);
                        await _context.SaveChangesAsync();

                        // Brand-new social account — send the welcome email below, once the
                        // rows are committed. Linking a provider to an existing account does not.
                        isNewUser = true;
                    }
                    else if (emailVerified && !user.IsEmailVerified)
                    {
                        // The provider vouches for this address, so any pending code flow is obsolete.
                        user.IsEmailVerified = true;
                        user.EmailVerificationCode = null;
                        user.EmailVerificationExpiry = null;
                    }

                    await _context.UserLogins.AddAsync(new UserLoginEntity
                    {
                        UserID = user.ID,
                        Provider = provider,
                        ProviderKey = providerKey,
                        RefreshToken = refreshToken
                    });

                    await _context.SaveChangesAsync();
                }
                else if (!string.IsNullOrWhiteSpace(refreshToken) && login is not null)
                {
                    // Apple issues a fresh refresh token on every authorization; keep the newest
                    // one so revoking at deletion targets a grant that is still valid.
                    login.RefreshToken = refreshToken;
                    await _context.SaveChangesAsync();
                }

                // Best-effort: a failed welcome email must never block the sign-in. Social
                // accounts get no verification email, so this is their only onboarding note.
                if (isNewUser)
                {
                    try
                    {
                        await _emailService.SendEmailAsync(
                            user.Email, EmailSubjects.Welcome, EmailTemplates.BuildWelcomeBody(user.Name), true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SocialSignin] Welcome email failed for {user.Email}: {ex.Message}");
                    }
                }

                if (user.IsEmailVerified is false)
                    return ResultWithDataDto<AuthResponseDto>.Failure(ErrorCodes.ValidateYourEmail);

                return GenerateAuthResponse(user);
            }
            catch (Exception)
            {
                return ResultWithDataDto<AuthResponseDto>.Failure(ErrorCodes.UnknownError);
            }
        }

        private ResultWithDataDto<AuthResponseDto> GenerateAuthResponse(UserEntity user)
        {
            var loggedInUser = new LoggedInUser(user.ID, user.Name, user.Email, user.IsEmailVerified);
            var token = _tokenService.GenerateJwt(loggedInUser);

            var authResponse = new AuthResponseDto(loggedInUser, token);

            return ResultWithDataDto<AuthResponseDto>.Success(authResponse);
        }

        public async Task<ResultDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto)
        {
            // 1️⃣ Validate user exists
            var normalizedEmail = EmailNormalizer.Normalize(dto.Email);
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

            if (user == null)
            {
                return ResultDto.Failure(ErrorCodes.EmailNotFoundError);
            }

            // 2️⃣ Generate secure reset token
            var resetToken = Guid.NewGuid().ToString("N"); // 32-character hex string
            var expiration = DateTime.UtcNow.AddHours(1); // Token valid for 1 hour

            // 3️⃣ Store or update token in database
            var existingToken = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(x => x.UserID == user.ID && !x.IsUsed);

            if (existingToken != null)
            {
                // Update existing token
                existingToken.Token = resetToken;
                existingToken.Expiration = expiration;
                existingToken.CreatedAt = DateTime.UtcNow;
            }
            else
            {
                // Create new token
                await _context.PasswordResetTokens.AddAsync(new PasswordResetTokenEntity
                {
                    UserID = user.ID,
                    Token = resetToken,
                    Expiration = expiration
                });
            }

            await _context.SaveChangesAsync();

            var frontendUrl = _configuration["AppSettings:FrontendUrl"];
            var resetLink = $"{frontendUrl}/reset-password.html?token={resetToken}";

            // 5️⃣ Send email with reset link
            try
            {
                var emailBody = $@"
<html>
  <body style='font-family: Arial, sans-serif; background-color: #f6f8fa; margin: 0; padding: 0;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='margin: 0; padding: 0;'>
      <tr>
        <td align='center' style='padding: 40px 0;'>
          <table width='500' cellpadding='0' cellspacing='0' style='background-color: #ffffff; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.05); padding: 30px; text-align: center;'>
            <tr>
              <td style='font-size: 24px; font-weight: bold; color: #333; padding-bottom: 10px;'>
                Password Reset Request
              </td>
            </tr>
            <tr>
              <td style='font-size: 16px; color: #555; padding-bottom: 20px;'>
                Hi {user.Name},
              </td>
            </tr>
            <tr>
              <td style='font-size: 15px; color: #555; line-height: 1.6; padding-bottom: 30px;'>
                You requested to reset your password. Please click the button below to reset it:
              </td>
            </tr>
            <tr>
              <td align='center' style='padding-bottom: 30px;'>
                <a href='{resetLink}'
                   style='background-color: #1b4965;
                          color: white;
                          padding: 12px 24px;
                          text-decoration: none;
                          border-radius: 5px;
                          font-size: 16px;
                          display: inline-block;'>
                  Reset Password
                </a>
              </td>
            </tr>
            <tr>
              <td style='font-size: 14px; color: #555; padding-bottom: 10px;'>
                This link will expire in 1 hour.
              </td>
            </tr>
            <tr>
              <td style='font-size: 14px; color: #555;'>
                If you did not request this, you can safely ignore this email.
              </td>
            </tr>
            <tr>
              <td style='padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #999;'>
                © {DateTime.UtcNow.Year} NavigationES
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </body>
</html>";

                var emailSent = await _emailService.SendEmailAsync(user.Email, "Password Reset Request", emailBody, true);

                if (!emailSent)
                    return ResultDto.Failure("Failed to send reset email. Please try again later.");

                return ResultDto.Success();
            }
            catch (Exception ex)
            {
                // Log the error but don't expose details to user
                Console.WriteLine($"Email send failed: {ex.Message}");
                return ResultDto.Failure("Failed to send reset email. Please try again later.");
            }
        }

        public async Task<ResultDto> ResetPasswordAsync(ResetPasswordRequestDto dto)
        {
            var resetToken = await _context.PasswordResetTokens
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Token == dto.Token && !x.IsUsed);

            if (resetToken == null) return ResultDto.Failure(ErrorCodes.ResetLinkInvalidError);

            // Check if token is expired
            if (resetToken.Expiration < DateTime.UtcNow) return ResultDto.Failure(ErrorCodes.ResetLinkExpiredError);

            // The reset page checks these too, but it is the only caller — a request
            // sent straight to the endpoint must not be able to set a weaker password
            // than the sign-up form accepts.
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
                return ResultDto.Failure(ErrorCodes.PasswordTooShortError);

            if (dto.NewPassword.Contains(' '))
                return ResultDto.Failure(ErrorCodes.PasswordHasSpacesError);

            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, @"[a-zA-Z]"))
                return ResultDto.Failure(ErrorCodes.PasswordMissingLetterError);

            if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, @"[!@#$%^&*\-_=+\[\]{};:'"",.<>?/\\|`~]"))
                return ResultDto.Failure(ErrorCodes.PasswordMissingSymbolError);

            // Get user and update password
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == resetToken.UserID);
            if (user == null) return ResultDto.Failure(ErrorCodes.UserDoesNotExist);

            // Generate new salt and hash for the new password
            (user.Salt, user.Hash) = _passwordService.GenerateSaltAndHash(dto.NewPassword);

            resetToken.IsUsed = true;

            try
            {
                await _context.SaveChangesAsync();
                return ResultDto.Success();
            }
            catch (Exception ex)
            {
                // The raw exception text used to be returned to the caller — on an
                // anonymous endpoint that hands schema details to anyone with a token.
                Console.WriteLine($"Password reset failed: {ex.Message}");
                return ResultDto.Failure(ErrorCodes.UnknownError);
            }
        }

        public async Task<ResultDto> ValidateCodeAsync(ValidationRequestDto validation)
        {
            if (validation == null)
                return ResultDto.Failure("Invalid request.");

            if (string.IsNullOrWhiteSpace(validation.Email) ||
                string.IsNullOrWhiteSpace(validation.Name) ||
                string.IsNullOrWhiteSpace(validation.ValidationCode))
            {
                return ResultDto.Failure("Name, Email, and Verification Code are all required.");
            }

            var normalizedEmail = EmailNormalizer.Normalize(validation.Email);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && u.Name == validation.Name && u.EmailVerificationCode == validation.ValidationCode);

            if (user == null)
                return ResultDto.Failure("Invalid verification details. Please check your information and try again.");

            if (user.IsEmailVerified)
                return ResultDto.Failure("Your email is already verified.");

            if (user.EmailVerificationExpiry == null || user.EmailVerificationExpiry < DateTime.UtcNow)
                return ResultDto.Failure("Verification code has expired. Please request a new one.");

            user.IsEmailVerified = true;
            user.EmailVerificationCode = null;
            user.EmailVerificationExpiry = null;

            try
            {
                await _context.SaveChangesAsync();

                var subject = EmailSubjects.EmailVerified;
                var body = EmailTemplates.BuildVerifiedBody(validation.Name);

                await _emailService.SendEmailAsync(user.Email, subject, body, true);

                return ResultDto.Success();
            }
            catch (Exception ex)
            {
                // Log internal error for debugging
                Console.WriteLine($"Email verification failed: {ex.Message}");
                return ResultDto.Failure("An error occurred while verifying your email. Please try again later.");
            }
        }

        // Permanently removes the account. The database cascades take everything
        // derived from it: UserLogins, PasswordResetTokens, TestSessions and — through
        // the sessions — SessionQuestions, SessionAnswers and ExamSessionAnswers.
        public async Task<ResultDto> DeleteAccountAsync(long userId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userId);
            if (user is null) return ResultDto.Failure(ErrorCodes.UserDoesNotExist);

            // Apple grants are revoked first so a returning user gets the first-time
            // consent screen again (App Store Guideline 5.1.1(v)). Best-effort: a
            // revoke failure must not leave the account undeleted.
            var appleLogins = await _context.UserLogins
                .Where(l => l.UserID == userId
                         && l.Provider == AppleAuthService.ProviderName
                         && l.RefreshToken != null)
                .ToListAsync();

            foreach (var login in appleLogins)
                await _appleAuthService.TryRevokeRefreshTokenAsync(login.RefreshToken!);

            try
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                return ResultDto.Success();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Account deletion failed for user {userId}: {ex.Message}");
                return ResultDto.Failure(ErrorCodes.UnknownError);
            }
        }
    }
}
