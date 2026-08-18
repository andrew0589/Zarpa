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
}
