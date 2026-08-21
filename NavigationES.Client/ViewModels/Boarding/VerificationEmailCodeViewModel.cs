using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;
using NavigationES.ApiClient;
using NavigationES.Client.Pages;
using NavigationES.Client.Resources.Languages;
using NavigationES.Client.Services;
using NavigationES.Client.Utilities;
using NavigationES.Shared.Dtos;

namespace NavigationES.Client.ViewModels.Boarding
{
    public partial class VerificationEmailCodeViewModel : BaseViewModel
    {
        private readonly IAuthApi _authApi;
        private readonly AuthService _authService;

        [ObservableProperty] private string _validationCode = string.Empty;
        [ObservableProperty] private string _code1 = string.Empty;
        [ObservableProperty] private string _code2 = string.Empty;
        [ObservableProperty] private string _code3 = string.Empty;
        [ObservableProperty] private string _code4 = string.Empty;
        [ObservableProperty] private string _code5 = string.Empty;
        [ObservableProperty] private string? _validationCodeErrorMessage;
        [ObservableProperty] private string _code6 = string.Empty;
        [ObservableProperty] private bool _showCodeError;

        public VerificationEmailCodeViewModel(IAuthApi authApi, AuthService authService)
        {
            _authApi = authApi;
            _authService = authService;
        }

        [RelayCommand]
        public async Task ValidateAsync()
        {
            IsBusy = true;

            if (!ValidateCode())
            {
                IsBusy = false;
                return;
            }

            try
            {
                var validation = new ValidationRequestDto
                {
                    Name = _authService.User!.Name,
                    Email = _authService.User.Email,
                    ValidationCode = ValidationCode,
                };
                var result = await _authApi.ValidateCodeAsync(validation);

                if (result.IsSuccess)
                {
                    await GoToAsync($"//{nameof(HomePage)}", animate: true);
                }
                else
                {
                    var message = BackendTranslator.Translate(result.ErrorMessage);
                    await UserMessageHelper.ShowErrorAsync(message ?? AppResources.SignupUnknownError);
                    IsBusy = false;
                }
            }
            catch (Exception ex)
            {
                await UserMessageHelper.ShowErrorAsync(ex.Message);
                IsBusy = false;
            }
        }

        private bool ValidateCode()
        {
            ShowCodeError = false;

            bool hasErrors = false;

            if (string.IsNullOrWhiteSpace(ValidationCode))
            {
                ValidationCodeErrorMessage = AppResources.CodeCannotBeEmpty;
                ShowCodeError = true;
                hasErrors = true;
            }

            if (!Regex.IsMatch(ValidationCode, @"^\d+$"))
            {
                ValidationCodeErrorMessage = AppResources.CodeMustContainOnlyNumbers;
                ShowCodeError = true;
                hasErrors = true;
            }

            if (ValidationCode.Length != 6)
            {
                ValidationCodeErrorMessage = AppResources.CodeMustBe6DigitsLong;
                ShowCodeError = true;
                hasErrors = true;
            }

            return !hasErrors;
        }
    }
}
