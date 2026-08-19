using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Refit;
using Zarpa.ApiClient;
using Zarpa.Web;
using Zarpa.Web.Auth;
using Zarpa.Web.Utilities;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// wwwroot/appsettings.json — the production build ships the real API domain there.
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:7136/";

builder.Services.AddSingleton(new ApiOptions(apiBaseUrl));

builder.Services.AddAuthorizationCore();
// One instance behind both faces: components inject WebAuthStateProvider to
// sign in/out, Blazor's [Authorize] machinery sees it as AuthenticationStateProvider.
builder.Services.AddSingleton<WebAuthStateProvider>();
builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<WebAuthStateProvider>());
builder.Services.AddTransient<AuthHeaderHandler>();

ConfigureRefit(builder.Services, apiBaseUrl);

await builder.Build().RunAsync();

static void ConfigureRefit(IServiceCollection services, string apiBaseUrl)
{
    // Same registration style as the MAUI head (MauiProgram.ConfigureRefit): the
    // source-generated clients from Zarpa.ApiClient, with the bearer handler on top.
    var refitSettings = new RefitSettings();

    services.AddRefitGeneratedClient<IAuthApi>(refitSettings)
        .ConfigureHttpClient(SetHttpClient)
        .AddHttpMessageHandler<AuthHeaderHandler>();

    services.AddRefitGeneratedClient<ITopicsApi>(refitSettings)
        .ConfigureHttpClient(SetHttpClient)
        .AddHttpMessageHandler<AuthHeaderHandler>();

    services.AddRefitGeneratedClient<ILicensesApi>(refitSettings)
        .ConfigureHttpClient(SetHttpClient)
        .AddHttpMessageHandler<AuthHeaderHandler>();

    services.AddRefitGeneratedClient<IComunidadesApi>(refitSettings)
        .ConfigureHttpClient(SetHttpClient)
        .AddHttpMessageHandler<AuthHeaderHandler>();

    services.AddRefitGeneratedClient<ISessionsApi>(refitSettings)
        .ConfigureHttpClient(SetHttpClient)
        .AddHttpMessageHandler<AuthHeaderHandler>();

    services.AddRefitGeneratedClient<IExamsApi>(refitSettings)
        .ConfigureHttpClient(SetHttpClient)
        .AddHttpMessageHandler<AuthHeaderHandler>();

    void SetHttpClient(HttpClient httpClient) => httpClient.BaseAddress = new Uri(apiBaseUrl);
}
