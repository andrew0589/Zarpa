using NavigationES.Client.ViewModels;

namespace NavigationES.Client.Pages;

public partial class StudyTopicPage : ContentPage
{
    private readonly StudyTopicViewModel _viewModel;

    public StudyTopicPage(StudyTopicViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    // Also on the way back from the player, so watched states are current.
    protected async override void OnAppearing()
    {
        base.OnAppearing();

        await _viewModel.LoadAsync();
    }
}
