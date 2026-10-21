using NavigationES.Client.ViewModels;

namespace NavigationES.Client.Pages;

public partial class StudyPage : ContentPage
{
    private readonly StudyViewModel _viewModel;

    public StudyPage(StudyViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    // Reloaded on every appearing so "Continuar viendo" follows the last video watched.
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadAsync();
    }

    protected override void OnDisappearing()
    {
        // Known Android RefreshView issue (see TopicPracticePage).
        _viewModel.IsRefreshing = false;
        base.OnDisappearing();
    }
}
