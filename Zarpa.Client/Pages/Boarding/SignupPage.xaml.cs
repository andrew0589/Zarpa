using Zarpa.Client.ViewModels;

namespace Zarpa.Client.Pages;

public partial class SignupPage : ContentPage
{
    public SignupPage(AuthViewModel authViewModel)
    {
        InitializeComponent();
        BindingContext = authViewModel;
    }

    private async void SigninLabel_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//{nameof(SigninPage)}");
    }

    private void TogglePasswordVisibility_Tapped(object sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        PasswordEyeIcon.Text = PasswordEntry.IsPassword
            ? UraniumUI.Icons.FontAwesome.Solid.Eye
            : UraniumUI.Icons.FontAwesome.Solid.EyeSlash;
    }
}
