using NavigationES.Client.ViewModels;

namespace NavigationES.Client.Pages;

public partial class ExamSessionPage : ContentPage
{
    private readonly ExamSessionViewModel _viewModel;

    public ExamSessionPage(ExamSessionViewModel viewModel)
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

    protected override void OnDisappearing()
    {
        // Stops the countdown while the page is out of view; StartAsync resumes it
        // when the page (or the app) comes back.
        _viewModel.PauseTimer();
        base.OnDisappearing();
    }

    // Android hardware back: same confirmation as the nav-bar arrow.
    protected override bool OnBackButtonPressed()
    {
        if (!_viewModel.IsRunning)
            return base.OnBackButtonPressed();

        _ = _viewModel.ExitCommand.ExecuteAsync(null);
        return true;
    }
}
