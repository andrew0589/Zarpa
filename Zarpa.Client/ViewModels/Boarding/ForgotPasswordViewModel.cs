using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Refit;
using Zarpa.Client.Pages;
using Zarpa.Client.Resources.Languages;
using Zarpa.Client.Services;
using Zarpa.Client.Utilities;
using Zarpa.Shared.Dtos;

namespace Zarpa.Client.ViewModels
{
    public partial class ForgotPasswordViewModel : BaseViewModel
    {
        private readonly IAuthApi _authApi;
        public UserSessionService Session { get; }

        [ObservableProperty] private string? _email;
        [ObservableProperty] private bool _isSuccessVisible;

        public ForgotPasswordViewModel(IAuthApi authApi, UserSessionService session)
        {
            _authApi = authApi;
            Session = session;
        }

        [RelayCommand]
        private async Task SubmitAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                await UserMessageHelper.ShowErrorAsync(AppResources.InvalidEmailAddress);
                return;
            }

            // Check rate limiting
            const string requestTimestampsKey = "ForgotPassword_Timestamps";
            var now = DateTime.UtcNow;
            var timestampsJson = Preferences.Get(requestTimestampsKey, string.Empty);
            var timestamps = string.IsNullOrEmpty(timestampsJson)
                ? new List<DateTime>()
                : System.Text.Json.JsonSerializer.Deserialize<List<DateTime>>(timestampsJson) ?? new List<DateTime>();

            // Remove timestamps older than 1 hour
            timestamps = timestamps.Where(t => (now - t).TotalHours < 1).ToList();

            if (timestamps.Count >= 2)
            {
                await UserMessageHelper.ShowErrorAsync("You can only send 2 reset requests per hour. Please try again later.");
                return;
            }

            IsBusy = true;

            try
            {
                var forgotPass = new ForgotPasswordRequestDto
                {
                    Email = Email,
                };

                var result = await _authApi.ForgotPasswordAsync(forgotPass);

                if (result.IsSuccess)
                {
                    IsSuccessVisible = true;
                    timestamps.Add(now);
                    Preferences.Set(requestTimestampsKey, System.Text.Json.JsonSerializer.Serialize(timestamps));
                }
                else
                {
                    var message = BackendTranslator.Translate(result.ErrorMessage);
                    await UserMessageHelper.ShowErrorAsync(message);
                }
            }
            catch (ApiException apiEx)
            {
                await UserMessageHelper.ShowErrorAsync(apiEx.Content ?? apiEx.Message);
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
        private async Task GoToSigninAsync()
        {
            await Shell.Current.GoToAsync($"//{nameof(SigninPage)}");
        }
    }
}
