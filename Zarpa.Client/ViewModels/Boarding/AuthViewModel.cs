using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Zarpa.ApiClient;
using Zarpa.Client.Pages;
using Zarpa.Client.Pages.Boarding;
using Zarpa.Client.Resources.Languages;
using Zarpa.Client.Services;
using Zarpa.Client.Services.Environment;
using Zarpa.Client.Utilities;
using Zarpa.Shared.Constants;
using Zarpa.Shared.Dtos;

namespace Zarpa.Client.ViewModels
{
    public partial class AuthViewModel(UserSessionService session, IAuthApi authApi, AuthService authService, IEnvironmentService environmentService) : BaseViewModel
    {
        private readonly IAuthApi _authApi = authApi;
        private readonly AuthService _authService = authService;
        private readonly IEnvironmentService _environmentService = environmentService;
        public UserSessionService Session { get; } = session;

        [ObservableProperty] private string? _name;
        [ObservableProperty] private string? _email;
        [ObservableProperty] private string? _password;
        [ObservableProperty] private string? _confirmPassword;
        [ObservableProperty] private bool _isTermsAndConditionsAccepted;
        [ObservableProperty] private string? _nameErrorMessage;
        [ObservableProperty] private bool _showNameError;
        [ObservableProperty] private string? _emailErrorMessage;
        [ObservableProperty] private bool _showEmailError;
        [ObservableProperty] private string? _passwordErrorMessage;
        [ObservableProperty] private bool _showPasswordError;
        [ObservableProperty] private string? _confirmPasswordErrorMessage;
        [ObservableProperty] private bool _showConfirmPasswordError;
        [ObservableProperty] private string? _termsErrorMessage;
        [ObservableProperty] private bool _showTermsError;

        [RelayCommand]
        private async Task SignupAsync()
        {
            IsBusy = true;

            if (!ValidateSignupForm())
            {
                IsBusy = false;
                return;
            }

            try
            {
                var signupDto = new SignupRequestDto(Name!, Email!, Password!);
                //Make Api Call
                var result = await _authApi.SignupAsync(signupDto);

                if (result.IsSuccess)
                {
                    _authService.Signin(result.Data);

                    await Shell.Current.GoToAsync(nameof(VerificationEmailCodePage));
                }
                else
                {
                    var message = BackendTranslator.Translate(result.ErrorCode);
                    await UserMessageHelper.ShowErrorAsync(message ?? AppResources.SignupUnknownError);
                }
            }
            catch (Exception ex)
            {
                await UserMessageHelper.ShowErrorAsync(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SigninAsync()
        {
            IsBusy = true;

            if (!ValidateSigninForm())
            {
                IsBusy = false;
                return;
            }

            try
            {
                var signinDto = new SigninRequestDto(Email!, Password!);
                //Make Api Call
                var result = await _authApi.SigninAsync(signinDto);

                if (result.IsSuccess)
                {
                    _authService.Signin(result.Data);

                    //Navigate to HomePage
                    await GoToAsync("//HomePage");
                }
                else
                {
                    var message = BackendTranslator.Translate(result.ErrorCode);
                    await UserMessageHelper.ShowErrorAsync(message ?? AppResources.SigninUnknownError);
                }
            }
            catch (Exception ex)
            {
                var detail = ex.InnerException?.Message;
                var fullMessage = string.IsNullOrWhiteSpace(detail) ? ex.Message : $"{ex.Message}\n\n{detail}";
                await ShowErrorAlertAsync(fullMessage);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private Task GoogleSigninAsync() => SocialSigninAsync("google", ErrorCodes.GoogleAuthFailedError);

        [RelayCommand]
        private Task AppleSigninAsync() => SocialSigninAsync("apple", ErrorCodes.AppleAuthFailedError);

        [RelayCommand]
        private Task FacebookSigninAsync() => SocialSigninAsync("facebook", ErrorCodes.FacebookAuthFailedError);

        private async Task SocialSigninAsync(string provider, string authFailedErrorCode)
        {
            if (IsBusy) return;
            IsBusy = true;

            try
            {
                // The whole OAuth flow happens between the system browser and the API;
                // the app only opens /start and waits for the zarpa:// callback.
                var startUrl = $"{_environmentService.ApiBaseUrl.TrimEnd('/')}/api/auth/{provider}/start";

                var result = await WebAuthenticator.Default.AuthenticateAsync(
                    new Uri(startUrl),
                    new Uri("zarpa://auth-callback"));

                if (result.Properties.TryGetValue("error", out var errorCode))
                {
                    await UserMessageHelper.ShowErrorAsync(BackendTranslator.Translate(errorCode));
                    return;
                }

                if (!result.Properties.TryGetValue("token", out var token) ||
                    !result.Properties.TryGetValue("userId", out var userIdRaw) ||
                    !long.TryParse(userIdRaw, out var userId))
                {
                    await UserMessageHelper.ShowErrorAsync(BackendTranslator.Translate(authFailedErrorCode));
                    return;
                }

                result.Properties.TryGetValue("name", out var name);
                result.Properties.TryGetValue("email", out var email);
                var verified = result.Properties.TryGetValue("verified", out var v) && v == "true";

                var loggedInUser = new LoggedInUser(userId, name ?? string.Empty, email ?? string.Empty, verified);
                _authService.Signin(new AuthResponseDto(loggedInUser, token));

                await GoToAsync("//HomePage");
            }
            catch (TaskCanceledException)
            {
                // User closed the browser without finishing the login — nothing to report.
            }
            catch (Exception)
            {
                await UserMessageHelper.ShowErrorAsync(BackendTranslator.Translate(authFailedErrorCode));
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ForgotPasswordAsync()
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(ForgotPasswordPage));
            }
            catch (Exception ex)
            {
                await ShowErrorAlertAsync(ex.Message);
            }
        }

        private bool ValidateSigninForm()
        {
            // Clear previous errors
            ShowEmailError = false;
            ShowPasswordError = false;

            bool hasErrors = false;

            // Validate email
            if (!IsValidEmail(Email))
            {
                EmailErrorMessage = AppResources.AddValidMailMessage;
                ShowEmailError = true;
                hasErrors = true;
            }

            // Validate password
            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordErrorMessage = AppResources.IncorrectPasswordError;
                ShowPasswordError = true;
                hasErrors = true;
            }

            return !hasErrors;
        }

        private bool ValidateSignupForm()
        {
            // Clear previous errors
            ShowNameError = false;
            ShowEmailError = false;
            ShowPasswordError = false;
            ShowConfirmPasswordError = false;
            ShowTermsError = false;

            bool hasErrors = false;

            // Validate name
            if (string.IsNullOrWhiteSpace(Name))
            {
                NameErrorMessage = AppResources.FieldNameNotValidMessage;
                ShowNameError = true;
                hasErrors = true;
            }

            // Validate email
            if (!IsValidEmail(Email))
            {
                EmailErrorMessage = AppResources.AddValidMailMessage;
                ShowEmailError = true;
                hasErrors = true;
            }

            // Validate password
            if (string.IsNullOrWhiteSpace(Password))
            {
                PasswordErrorMessage = AppResources.IncorrectPasswordError;
                ShowPasswordError = true;
                hasErrors = true;
            }
            else if (Password.Length < 6)
            {
                PasswordErrorMessage = AppResources.PasswordTooShort;
                ShowPasswordError = true;
                hasErrors = true;
            }
            else if (Password.Contains(' '))
            {
                PasswordErrorMessage = AppResources.PasswordContainsSpaces;
                ShowPasswordError = true;
                hasErrors = true;
            }

            // Validate password confirmation
            if (Password != ConfirmPassword)
            {
                ConfirmPasswordErrorMessage = AppResources.PasswordsDoNotMatch;
                ShowConfirmPasswordError = true;
                hasErrors = true;
            }

            // Validate terms and conditions
            if (!IsTermsAndConditionsAccepted)
            {
                TermsErrorMessage = AppResources.TermsAndConditionsAgreement;
                ShowTermsError = true;
                hasErrors = true;
            }

            return !hasErrors;
        }
    }
}
