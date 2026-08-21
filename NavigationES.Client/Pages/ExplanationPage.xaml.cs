using NavigationES.Client.ViewModels;

namespace NavigationES.Client.Pages;

public partial class ExplanationPage : ContentPage
{
    public ExplanationPage(ExplanationViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}
