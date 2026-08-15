using Zarpa.Client.ViewModels;

namespace Zarpa.Client.Pages;

public partial class SigninPage : ContentPage
{
    public SigninPage(AuthViewModel authViewModel)
    {
        InitializeComponent();

        BindingContext = authViewModel;
    }

    private async void SignupLabel_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SignupPage));
    }
}
