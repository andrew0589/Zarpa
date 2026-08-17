using Zarpa.Client.Pages;
using Zarpa.Client.Pages.Boarding;

namespace Zarpa.Client;

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
