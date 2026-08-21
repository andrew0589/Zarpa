using NavigationES.Client.Services;
using NavigationES.Client.ViewModels;

namespace NavigationES.Client.Pages;

public partial class SigninPage : ContentPage
{
    private readonly AuthService _authService;

    public SigninPage(AuthViewModel authViewModel, AuthService authService)
    {
        InitializeComponent();

        BindingContext = authViewModel;
        _authService = authService;
    }

    // Moved from the former OnboardingPage: a persisted valid session skips sign-in entirely.
    protected async override void OnAppearing()
    {
        if (_authService.User is not null && _authService.User.Id > 0
            && !string.IsNullOrWhiteSpace(_authService.Token) &&
             _authService.User.IsEmailVerified)
        {
            await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
        }
    }

    private async void SignupLabel_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SignupPage));
    }

    private void TogglePasswordVisibility_Tapped(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordEyeIcon.Text = PasswordEntry.IsPassword
            ? UraniumUI.Icons.FontAwesome.Solid.Eye
            : UraniumUI.Icons.FontAwesome.Solid.EyeSlash;
    }
}
