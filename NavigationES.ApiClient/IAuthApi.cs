using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
{
    public interface IAuthApi
    {
        [Post("/api/signup")]
        Task<ResultWithDataDto<AuthResponseDto>> SignupAsync(SignupRequestDto dto);

        [Post("/api/signin")]
        Task<ResultWithDataDto<AuthResponseDto>> SigninAsync(SigninRequestDto dto);

        [Post("/api/forgotPassword")]
        Task<ResultDto> ForgotPasswordAsync(ForgotPasswordRequestDto dto);

        [Post("/api/checkValidationCode")]
        Task<ResultDto> ValidateCodeAsync(ValidationRequestDto validation);

        // Requires the bearer token (AuthHeaderHandler adds it); deletes the
        // signed-in user's account and all of their data.
        [Delete("/api/account")]
        Task<ResultDto> DeleteAccountAsync();
    }
}
