using Zarpa.Client.Services;
using Zarpa.Client.Utilities;

namespace Zarpa.Client.Pages;

public partial class HomePage : ContentPage
{
    private readonly AuthService _authService;
    private readonly UserSessionService _session;

    public HomePage(AuthService authService, UserSessionService session)
    {
        InitializeComponent();

        _authService = authService;
        _session = session;
    }

    private async void Signout_Clicked(object sender, EventArgs e)
    {
        _authService.Signout();
        _session.Clear();

        await Shell.Current.GoToAsync($"//{nameof(OnboardingPage)}");
    }
}
