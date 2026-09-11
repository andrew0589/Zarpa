using NavigationES.Client.ViewModels;

namespace NavigationES.Client.Pages;

public partial class HomePage : ContentPage
{
    private readonly HomeViewModel _viewModel;

    public HomePage(HomeViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected async override void OnAppearing()
    {
        base.OnAppearing();

        // Reloads on every return so the progress reflects what was just practiced.
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
