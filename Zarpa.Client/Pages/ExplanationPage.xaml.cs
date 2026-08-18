using Zarpa.Client.ViewModels;

namespace Zarpa.Client.Pages;

public partial class ExplanationPage : ContentPage
{
    public ExplanationPage(ExplanationViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}
