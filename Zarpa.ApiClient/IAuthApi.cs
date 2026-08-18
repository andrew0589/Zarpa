using Refit;
using Zarpa.Shared.Dtos;

namespace Zarpa.ApiClient
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
    }
}
