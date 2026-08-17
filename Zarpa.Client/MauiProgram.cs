using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Refit;
using UraniumUI;
using Zarpa.Client.Pages;
using Zarpa.Client.Pages.Boarding;
using Zarpa.Client.Services;
using Zarpa.Client.Services.Environment;
using Zarpa.Client.Utilities;
using Zarpa.Client.ViewModels;
using Zarpa.Client.ViewModels.Boarding;

#if ANDROID
using System.Net.Security;
using Xamarin.Android.Net;
#elif IOS
using Security;
#endif

namespace Zarpa.Client;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                // Field icons on the sign-in card (envelope, lock, eye).
                fonts.AddFontAwesomeIconFonts();
            })
            .UseMauiCommunityToolkit()
            .UseUraniumUI()
            .UseUraniumUIMaterial();

#if DEBUG
        builder.Logging.AddDebug();
        builder.Services.AddSingleton<IEnvironmentService, DevelopmentEnvironmentService>();
#else
        builder.Services.AddSingleton<IEnvironmentService, ProductionEnvironmentService>();
#endif

        // Register ViewModels
        builder.Services.AddTransient<AuthViewModel>();
        builder.Services.AddTransient<ForgotPasswordViewModel>();
        builder.Services.AddTransient<VerificationEmailCodeViewModel>();
        builder.Services.AddTransient<TopicPracticeViewModel>();
        builder.Services.AddTransient<TestsViewModel>();
        builder.Services.AddTransient<TopicSessionViewModel>();

        // Register Services
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<UserSessionService>();
        builder.Services.AddSingleton<SelectedLicenseService>();

        // Register Pages
        builder.Services.AddTransient<SigninPage>();
        builder.Services.AddTransient<SignupPage>();
        builder.Services.AddTransient<ForgotPasswordPage>();
        builder.Services.AddTransient<VerificationEmailCodePage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<TestsPage>();
        builder.Services.AddTransient<StudyPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<TopicPracticePage>();
        builder.Services.AddTransient<TopicSessionPage>();
        builder.Services.AddTransient<ExamPracticePage>();

        ConfigureRefit(builder.Services);

        var app = builder.Build();
        ServiceHelper.Initialize(app.Services);
        return app;
    }

    private static void ConfigureRefit(IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        var envService = provider.GetRequiredService<IEnvironmentService>();
        var refitSettings = new RefitSettings
        {
            HttpMessageHandlerFactory = () =>
            {
                HttpMessageHandler primary;
#if ANDROID
                primary = new AndroidMessageHandler
                {
#if DEBUG
                    // DEBUG only: accept the dev API's self-signed certificate, but only for
                    // local development hosts. Standard OS validation applies everywhere else.
                    // Never `return true` unconditionally — that disables TLS validation app-wide.
                    ServerCertificateCustomValidationCallback = (httpRequestMessage, certificate, chain, sslPolicyErrors) =>
                    {
                        if (sslPolicyErrors == SslPolicyErrors.None)
                            return true;

                        var host = httpRequestMessage?.RequestUri?.Host;
                        return host is "localhost" or "127.0.0.1" or "10.0.2.2";
                    }
#endif
                };
#elif IOS
                primary = new NSUrlSessionHandler
                {
                    TrustOverrideForUrl = (NSUrlSessionHandler sender, string url, SecTrust trust) =>
                    {
#if DEBUG
                        // DEBUG only: trust the dev API's self-signed localhost certificate.
                        if (url.StartsWith("https://localhost"))
                            return true;
#endif
                        // Fall back to standard OS certificate validation for everything else.
                        // Returning false here (instead of deferring) would reject even valid certs.
                        return trust.Evaluate(out _);
                    }
                };
#else
                primary = new HttpClientHandler();
#endif
                // Outermost handler adds the bearer token (and handles 401).
                return new AuthHeaderHandler
                {
                    InnerHandler = primary
                };
            }
        };

        // The source-generated client: Refit 15 split the runtime reflection builder into
        // a separate Refit.Reflection package, so plain AddRefitClient throws at runtime.
        services.AddRefitGeneratedClient<IAuthApi>(refitSettings)
            .ConfigureHttpClient(SetHttpClient);

        services.AddRefitGeneratedClient<ITopicsApi>(refitSettings)
            .ConfigureHttpClient(SetHttpClient);

        services.AddRefitGeneratedClient<ILicensesApi>(refitSettings)
            .ConfigureHttpClient(SetHttpClient);

        services.AddRefitGeneratedClient<ISessionsApi>(refitSettings)
            .ConfigureHttpClient(SetHttpClient);

        void SetHttpClient(HttpClient httpClient) => httpClient.BaseAddress = new Uri(envService.ApiBaseUrl);
    }
}
