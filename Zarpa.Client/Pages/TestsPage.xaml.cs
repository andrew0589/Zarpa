namespace Zarpa.Client.Pages;

public partial class TestsPage : ContentPage
{
    public TestsPage()
    {
        InitializeComponent();
    }

    private void ExamSimulation_Tapped(object sender, EventArgs e)
    {
        // TODO: navigate to the timed exam-simulation flow once it exists.
    }

    private async void LearningMode_Tapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(LearningModePage));
    }
}
