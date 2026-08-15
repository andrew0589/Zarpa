using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Zarpa.Api.Data;
using Zarpa.Api.Endpoints;
using Zarpa.Api.Services;
using Zarpa.Api.Utilities.Email;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

// Configure forwarded headers for proxy — the OAuth callback URLs are built from
// Request.Scheme/Host, so behind a reverse proxy these must reflect the public origin.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                             ForwardedHeaders.XForwardedProto |
                             ForwardedHeaders.XForwardedHost;
    // Clear known networks and proxies to allow all
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var connectionString = builder.Configuration.GetConnectionString("ZarpaDb");

builder.Services.AddDbContext<ZarpaDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure()
    ));

builder.Services.AddTransient<TokenService>();
builder.Services.AddTransient<PasswordService>();
builder.Services.AddTransient<AuthService>();
// Memory cache holds the short-lived OAuth "state" values between /start and /callback.
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<GoogleAuthService>();
builder.Services.AddHttpClient<AppleAuthService>();
builder.Services.AddHttpClient<FacebookAuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
    .AddJwtBearer(jwtOptions =>
    {
        jwtOptions.TokenValidationParameters = TokenService.GetTokenValidationParameters(builder.Configuration);
    });

// Secure by default: every endpoint requires a valid JWT unless it explicitly opts
// out with .AllowAnonymous() (sign-in/sign-up, social auth, legal pages, ping).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

Console.WriteLine($"ENVIRONMENT: {builder.Environment.EnvironmentName}");

var app = builder.Build();

// TODO: run EF Core migrations / database init here once a database is available,
// e.g. create migrations with `dotnet ef migrations add Initial` and then:
//   using var scope = app.Services.CreateScope();
//   scope.ServiceProvider.GetRequiredService<ZarpaDbContext>().Database.Migrate();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

// Use forwarded headers before other middleware
app.UseForwardedHeaders();

// Trust the proxy headers
app.Use((context, next) =>
{
    context.Request.Scheme = context.Request.Headers["X-Forwarded-Proto"].FirstOrDefault() ?? context.Request.Scheme;
    return next();
});

// Only use HTTPS redirection in production (let the reverse proxy handle TLS there)
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/ping", () => Results.Ok("pong")).AllowAnonymous();

app.MapAuthEndpoints();
app.MapLegalEndpoints();

app.Run();
