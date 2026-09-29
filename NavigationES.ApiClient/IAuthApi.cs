using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
{
    public interface IAuthApi
    {
        // recaptchaToken: the website's reCAPTCHA v3 token (X-Recaptcha-Token). The app
        // leaves it null — Refit then sends no header, and the API only asks browsers.
        [Post("/api/signup")]
        Task<ResultWithDataDto<AuthResponseDto>> SignupAsync(SignupRequestDto dto, [Header("X-Recaptcha-Token")] string? recaptchaToken = null);

        [Post("/api/signin")]
        Task<ResultWithDataDto<AuthResponseDto>> SigninAsync(SigninRequestDto dto);

        [Post("/api/forgotPassword")]
        Task<ResultDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto, [Header("X-Recaptcha-Token")] string? recaptchaToken = null);

        [Post("/api/checkValidationCode")]
        Task<ResultDto> ValidateCodeAsync(ValidationRequestDto validation);

        // Requires the bearer token (AuthHeaderHandler adds it); deletes the
        // signed-in user's account and all of their data.
        [Delete("/api/account")]
        Task<ResultDto> DeleteAccountAsync();

        // Renames the signed-in user; the answer is a refreshed session (user + token)
        // that replaces the stored one.
        [Put("/api/account/name")]
        Task<ResultWithDataDto<AuthResponseDto>> UpdateNameAsync(UpdateNameRequestDto dto);
    }
}
