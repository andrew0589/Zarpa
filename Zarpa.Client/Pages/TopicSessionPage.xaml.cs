using Zarpa.Client.ViewModels;

namespace Zarpa.Client.Pages;

public partial class TopicSessionPage : ContentPage
{
    private readonly TopicSessionViewModel _viewModel;

    public TopicSessionPage(TopicSessionViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.StartAsync();
    }

    // Swallows taps on the popup card so they don't reach the dimmed layer,
    // which would close the popup while the user reads or scrolls.
    private void ExplanationCard_Tapped(object sender, EventArgs e)
    {
    }
}
