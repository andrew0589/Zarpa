using NavigationES.ApiClient;
using NavigationES.Client.Pages;
using NavigationES.Client.Pages.Boarding;
using NavigationES.Client.Services;

namespace NavigationES.Client;

public partial class AppShell : Shell
{
    // The token the Estudiar tab was last decided for: one access call per sign-in.
    private string? _studyCheckedFor;

    public AppShell()
    {
        InitializeComponent();
        RegisterRoutes();
    }

    private static readonly Type[] _routablePageTypes =
        [
            typeof(SignupPage),
            typeof(ForgotPasswordPage),
            typeof(VerificationEmailCodePage),
            typeof(TopicPracticePage),
            typeof(TopicSessionPage),
            typeof(ExplanationPage),
            typeof(ExamsPage),
            typeof(ExamSessionPage),
            typeof(StudyTopicPage),
            typeof(StudyVideoPage),
        ];

    private static void RegisterRoutes()
    {
        foreach (var pageType in _routablePageTypes)
        {
            Routing.RegisterRoute(pageType.Name, pageType);
        }
    }

    protected override async void OnNavigated(ShellNavigatedEventArgs args)
    {
        base.OnNavigated(args);
        await RefreshStudyTabAsync();
    }

    // The API decides who sees Estudiar (administrators, or everyone once
    // Study:Public is on); signed out or a failed call hides the tab.
    private async Task RefreshStudyTabAsync()
    {
        var token = ServiceHelper.GetRequiredService<AuthService>().Token;
        if (token == _studyCheckedFor)
            return;
        _studyCheckedFor = token;

        if (string.IsNullOrEmpty(token))
        {
            StudyTab.IsVisible = false;
            return;
        }

        try
        {
            StudyTab.IsVisible = (await ServiceHelper.GetRequiredService<IStudyApi>().GetAccessAsync()).Enabled;
        }
        catch (Exception)
        {
            StudyTab.IsVisible = false;
            // Try again on the next navigation (offline at start-up, API restarting…).
            _studyCheckedFor = null;
        }
    }
}
