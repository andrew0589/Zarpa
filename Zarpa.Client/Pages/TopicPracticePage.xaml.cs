using Zarpa.Client.ViewModels;

namespace Zarpa.Client.Pages;

public partial class TopicPracticePage : ContentPage
{
    public TopicPracticePage(TopicPracticeViewModel viewModel)
    {
        InitializeComponent();

        BindingContext = viewModel;
    }
}
