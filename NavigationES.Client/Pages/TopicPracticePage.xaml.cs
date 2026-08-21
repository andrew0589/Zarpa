using NavigationES.Client.ViewModels;

namespace NavigationES.Client.Pages;

public partial class TopicPracticePage : ContentPage
{
    private readonly TopicPracticeViewModel _viewModel;

    public TopicPracticePage(TopicPracticeViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        // Known Android RefreshView issue: a spinner left active while the page's
        // platform views detach crashes with "PlatformView cannot be null here".
        _viewModel.IsRefreshing = false;
        base.OnDisappearing();
    }
}
