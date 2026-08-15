using Zarpa.Api.Services;
using Zarpa.Shared.Constants;
using Zarpa.Shared.Dtos;

namespace Zarpa.Api.Endpoints
{
    public static class AuthEndpoints
    {
        public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
        {
            // Authentication endpoints are the only ones reachable without a token —
            // everything else is covered by the fallback authorization policy.
            app.MapPost("/api/signup", async (SignupRequestDto dto, AuthService authService) =>
            TypedResults.Ok(await authService.SignupAsync(dto))).AllowAnonymous();

            app.MapPost("/api/signin", async (SigninRequestDto dto, AuthService authService) =>
            TypedResults.Ok(await authService.SigninAsync(dto))).AllowAnonymous();

            app.MapPost("/api/forgotPassword", async (ForgotPasswordRequestDto dto, AuthService authService) =>
            {
                return TypedResults.Ok(await authService.ForgotPasswordAsync(dto));
            }).AllowAnonymous();

            app.MapPost("/api/resetPassword", async (ResetPasswordRequestDto dto, AuthService authService) =>
            {
                return TypedResults.Ok(await authService.ResetPasswordAsync(dto));
            }).AllowAnonymous();

            app.MapPost("/api/checkValidationCode", async (ValidationRequestDto validation, AuthService authService) =>
            {
                return TypedResults.Ok(await authService.ValidateCodeAsync(validation));
            }).AllowAnonymous();

            // Opened in the system browser by WebAuthenticator; sends the user to Google's
            // consent page. The redirect_uri is this API's /callback below and must match
            // one of the Authorized redirect URIs of the Google OAuth client.
            app.MapGet("/api/auth/google/start", (HttpContext http, GoogleAuthService googleAuth) =>
                Results.Redirect(googleAuth.CreateAuthorizationUrl(GoogleCallbackUri(http)))).AllowAnonymous();

            // Google lands here with ?code&state. Exchange + validate, sign the user in,
            // then bounce back into the app via the zarpa:// scheme.
            app.MapGet("/api/auth/google/callback", async (HttpContext http, GoogleAuthService googleAuth, AuthService authService) =>
            {
                string? code = http.Request.Query["code"].FirstOrDefault();
                string? state = http.Request.Query["state"].FirstOrDefault();

                if (string.IsNullOrEmpty(code) || !googleAuth.ValidateState(state))
                    return AppErrorRedirect(ErrorCodes.GoogleAuthFailedError);

                var payload = await googleAuth.ExchangeCodeAsync(code, GoogleCallbackUri(http));
                if (payload is null)
                    return AppErrorRedirect(ErrorCodes.GoogleAuthFailedError);

                var result = await authService.SocialSigninAsync(
                    GoogleAuthService.ProviderName, payload.Subject, payload.Email, payload.Name, payload.EmailVerified);

                return result.IsSuccess
                    ? AppSuccessRedirect(result.Data)
                    : AppErrorRedirect(result.ErrorCode ?? ErrorCodes.GoogleAuthFailedError);
            }).AllowAnonymous();

            // Same server-driven flow with Apple. Two twists: requesting the name/email
            // scopes forces response_mode=form_post, so the callback is a POST reading form
            // fields, and the user's name arrives only on the very first authorization.
            app.MapGet("/api/auth/apple/start", (HttpContext http, AppleAuthService appleAuth) =>
                Results.Redirect(appleAuth.CreateAuthorizationUrl(AppleCallbackUri(http)))).AllowAnonymous();

            app.MapPost("/api/auth/apple/callback", async (HttpContext http, AppleAuthService appleAuth, AuthService authService) =>
            {
                var form = await http.Request.ReadFormAsync();
                string? code = form["code"].FirstOrDefault();
                string? state = form["state"].FirstOrDefault();

                if (string.IsNullOrEmpty(code) || !appleAuth.ValidateState(state))
                    return AppErrorRedirect(ErrorCodes.AppleAuthFailedError);

                var payload = await appleAuth.ExchangeCodeAsync(code, AppleCallbackUri(http));
                if (payload is null)
                    return AppErrorRedirect(ErrorCodes.AppleAuthFailedError);

                var result = await authService.SocialSigninAsync(
                    AppleAuthService.ProviderName, payload.Subject, payload.Email,
                    AppleAuthService.ParseUserName(form["user"].FirstOrDefault()), payload.EmailVerified,
                    payload.RefreshToken);

                return result.IsSuccess
                    ? AppSuccessRedirect(result.Data)
                    : AppErrorRedirect(result.ErrorCode ?? ErrorCodes.AppleAuthFailedError);
            }).AllowAnonymous();

            // Same server-driven flow with Facebook. Its twist: no id_token — identity is
            // fetched from Graph /me with the access token, and the email can be missing
            // (denied permission or phone-only account), which we treat as a failure since
            // accounts are linked by email.
            app.MapGet("/api/auth/facebook/start", (HttpContext http, FacebookAuthService facebookAuth) =>
                Results.Redirect(facebookAuth.CreateAuthorizationUrl(FacebookCallbackUri(http)))).AllowAnonymous();

            app.MapGet("/api/auth/facebook/callback", async (HttpContext http, FacebookAuthService facebookAuth, AuthService authService) =>
            {
                string? code = http.Request.Query["code"].FirstOrDefault();
                string? state = http.Request.Query["state"].FirstOrDefault();

                if (string.IsNullOrEmpty(code) || !facebookAuth.ValidateState(state))
                    return AppErrorRedirect(ErrorCodes.FacebookAuthFailedError);

                var payload = await facebookAuth.ExchangeCodeAsync(code, FacebookCallbackUri(http));
                if (payload is null)
                    return AppErrorRedirect(ErrorCodes.FacebookAuthFailedError);

                if (string.IsNullOrEmpty(payload.Email))
                    return AppErrorRedirect(ErrorCodes.FacebookNoEmailError);

                // Facebook only returns emails it has confirmed, so skip the 6-digit code flow.
                var result = await authService.SocialSigninAsync(
                    FacebookAuthService.ProviderName, payload.Subject, payload.Email, payload.Name, emailVerified: true);

                return result.IsSuccess
                    ? AppSuccessRedirect(result.Data)
                    : AppErrorRedirect(result.ErrorCode ?? ErrorCodes.FacebookAuthFailedError);
            }).AllowAnonymous();

            static string GoogleCallbackUri(HttpContext http) =>
                $"{http.Request.Scheme}://{http.Request.Host}/api/auth/google/callback";

            static string AppleCallbackUri(HttpContext http) =>
                $"{http.Request.Scheme}://{http.Request.Host}/api/auth/apple/callback";

            static string FacebookCallbackUri(HttpContext http) =>
                $"{http.Request.Scheme}://{http.Request.Host}/api/auth/facebook/callback";

            static IResult AppSuccessRedirect(AuthResponseDto auth) =>
                Results.Redirect($"{SocialAuth.AppCallbackUrl}" +
                    $"?token={Uri.EscapeDataString(auth.Token)}" +
                    $"&userId={auth.user.Id}" +
                    $"&name={Uri.EscapeDataString(auth.user.Name)}" +
                    $"&email={Uri.EscapeDataString(auth.user.Email)}" +
                    $"&verified={(auth.user.IsEmailVerified ? "true" : "false")}");

            static IResult AppErrorRedirect(string errorCode) =>
                Results.Redirect($"{SocialAuth.AppCallbackUrl}?error={Uri.EscapeDataString(errorCode)}");

            return app;
        }
    }
}
