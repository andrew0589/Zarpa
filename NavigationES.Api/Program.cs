using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using NavigationES.Api.Data;
using NavigationES.Api.Data.Repositories;
using NavigationES.Api.Endpoints;
using NavigationES.Api.Services;
using NavigationES.Api.Utilities.Email;

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

var connectionString = builder.Configuration.GetConnectionString("NavigationESDb");

builder.Services.AddDbContext<NavigationESDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
        sqlOptions.EnableRetryOnFailure()
    ));

builder.Services.AddTransient<TokenService>();
builder.Services.AddTransient<PasswordService>();
builder.Services.AddTransient<AuthService>();
builder.Services.AddTransient<TopicService>();
builder.Services.AddTransient<LicenseService>();
builder.Services.AddTransient<ComunidadService>();
builder.Services.AddTransient<ExamService>();
builder.Services.AddTransient<ExamSessionService>();
builder.Services.AddTransient<PracticeSessionService>();
builder.Services.AddTransient<QuestionImportService>();
builder.Services.AddTransient<ExamImportService>();

// Repositories: all data access (the LINQ queries) lives here; services hold the
// business rules and endpoints only adapt HTTP. Scoped to follow the DbContext.
builder.Services.AddScoped<ITopicRepository, TopicRepository>();
builder.Services.AddScoped<IQuestionRepository, QuestionRepository>();
builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
builder.Services.AddScoped<ISessionRepository, SessionRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExamRepository, ExamRepository>();
builder.Services.AddScoped<IExamSessionRepository, ExamSessionRepository>();
// Memory cache holds the short-lived OAuth "state" values between /start and /callback.
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<GoogleAuthService>();
builder.Services.AddHttpClient<AppleAuthService>();
builder.Services.AddHttpClient<FacebookAuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// CORS: only the browser-based client (NavigationES.Web) needs it — native apps are exempt
// from the same-origin policy. Origins come from config so production can add the
// real domain later without a code change; no origins configured = no CORS at all.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
if (corsOrigins.Length > 0)
{
    builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));
}

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

// Bring the schema up to date on startup: a fresh deployment has an empty database
// and nothing else runs `dotnet ef database update` on the server. Idempotent — it
// applies only the migrations the database is missing. EnableRetryOnFailure on the
// DbContext covers the case where SQL Server is still coming up.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<NavigationESDbContext>().Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
    // Interactive API UI at /scalar/v1 — the manual way to call the import endpoint.
    app.MapScalarApiReference().AllowAnonymous();
}

// Content management (question/exam import). Reachable in every environment but
// guarded by the X-Admin-Key header (AdminKeyEndpointFilter), which fails closed
// when no key is configured.
app.MapAdminEndpoints();

// Use forwarded headers before other middleware
app.UseForwardedHeaders();

// Question/explanation images live in wwwroot/images and are served publicly —
// static files bypass the endpoint auth policy by design.
app.UseStaticFiles();

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

if (corsOrigins.Length > 0)
{
    app.UseCors();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/ping", () => Results.Ok("pong")).AllowAnonymous();

app.MapAuthEndpoints();
app.MapLegalEndpoints();
app.MapTopicEndpoints();
app.MapLicenseEndpoints();
app.MapComunidadEndpoints();
app.MapExamEndpoints();
app.MapSessionEndpoints();

app.Run();
