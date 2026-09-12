using NavigationES.Client.ViewModels;

namespace NavigationES.Client.Pages;

public partial class ExamsPage : ContentPage
{
    private readonly ExamsViewModel _viewModel;

    public ExamsPage(ExamsViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        // Reloads on every return so a just-finished paper shows its new status.
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
