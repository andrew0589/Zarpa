using Zarpa.Client.Services;
using Zarpa.Client.Utilities;

namespace Zarpa.Client.Pages;

public partial class HomePage : ContentPage
{
    private readonly AuthService _authService;
    private readonly UserSessionService _session;
    private readonly SelectedLicenseService _selectedLicense;

    public HomePage(AuthService authService, UserSessionService session, SelectedLicenseService selectedLicense)
    {
        InitializeComponent();

        _authService = authService;
        _session = session;
        _selectedLicense = selectedLicense;
    }

    private async void Signout_Clicked(object sender, EventArgs e)
    {
        _authService.Signout();
        _session.Clear();
        // The license choice lives on the account; the local cache must not leak
        // into whoever signs in next on this device.
        _selectedLicense.Clear();

        await Shell.Current.GoToAsync($"//{nameof(SigninPage)}");
    }
}
