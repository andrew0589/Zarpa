using Zarpa.Client.Resources.Languages;
using Zarpa.Client.Utilities;
using Zarpa.Client.ViewModels;

namespace Zarpa.Client.Pages;

public partial class TestsPage : ContentPage
{
    private readonly TestsViewModel _viewModel;

    public TestsPage(TestsViewModel viewModel)
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

    // Every mode needs to know which qualification the session belongs to.
    private async Task<bool> EnsureLicenseSelectedAsync()
    {
        if (_viewModel.SelectedLicenseId is not null)
            return true;

        await UserMessageHelper.ShowWarningAsync(AppResources.SelectLicenseFirst);
        return false;
    }

    private async void ExamSimulation_Tapped(object sender, EventArgs e)
    {
        if (!await EnsureLicenseSelectedAsync())
            return;

        // TODO: navigate to the timed exam-simulation flow once it exists.
    }

    private async void PracticeByTopic_Clicked(object sender, EventArgs e)
    {
        if (!await EnsureLicenseSelectedAsync())
            return;

        await Shell.Current.GoToAsync(nameof(TopicPracticePage));
    }

    private async void PracticeLikeExam_Clicked(object sender, EventArgs e)
    {
        if (!await EnsureLicenseSelectedAsync())
            return;

        await Shell.Current.GoToAsync(nameof(ExamPracticePage));
    }
}
