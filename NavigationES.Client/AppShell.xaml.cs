using NavigationES.Client.Pages;
using NavigationES.Client.Pages.Boarding;

namespace NavigationES.Client;

public partial class AppShell : Shell
{
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
            typeof(ExamPracticePage),
        ];

    private static void RegisterRoutes()
    {
        foreach (var pageType in _routablePageTypes)
        {
            Routing.RegisterRoute(pageType.Name, pageType);
        }
    }
}
