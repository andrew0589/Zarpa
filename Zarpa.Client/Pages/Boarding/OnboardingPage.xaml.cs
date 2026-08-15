using Zarpa.Client.Services;

namespace Zarpa.Client.Pages;

public partial class OnboardingPage : ContentPage
{
    private readonly AuthService _authService;

    public OnboardingPage(AuthService authService)
    {
        InitializeComponent();

        _authService = authService;
    }

    protected async override void OnAppearing()
    {
        if (_authService.User is not null && _authService.User.Id > 0
            && !string.IsNullOrWhiteSpace(_authService.Token) &&
             _authService.User.IsEmailVerified)
        {
            await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
        }
    }

    private async void SignIn_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("SigninPage");
    }

    private async void SignUp_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("SignupPage");
    }
}
