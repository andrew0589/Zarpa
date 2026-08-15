namespace Zarpa.Client.Pages;

public partial class LearningModePage : ContentPage
{
    public LearningModePage()
    {
        InitializeComponent();
    }

    private async void PracticeByTopic_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(TopicPracticePage));
    }

    private async void PracticeLikeExam_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(ExamPracticePage));
    }
}
