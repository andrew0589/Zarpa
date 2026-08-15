using CommunityToolkit.Mvvm.ComponentModel;
using System.Net.Mail;

namespace Zarpa.Client.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty] private bool _isBusy;

        [ObservableProperty] private bool _isRefreshing;

        public BaseViewModel()
        {
        }

        protected async Task GoToAsync(string url, bool animate = false) =>
            await Shell.Current.GoToAsync(url, animate);

        protected async Task ShowErrorAlertAsync(string errorMessage)
        {
            // Guard against Shell not being ready yet
            if (Shell.Current is null) return;
            await Shell.Current.DisplayAlert("Error", errorMessage, "Ok");
        }

        #region validation fields
        public bool IsValidEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var mailAddress = new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}
