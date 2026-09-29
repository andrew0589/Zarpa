using NavigationES.Api.Services;
using NavigationES.Shared.Constants;

namespace NavigationES.Api.Auth
{
    // reCAPTCHA v3 gate for the website's signup and forgot-password calls.
    //
    // Only browser requests are checked — they always carry an Origin header on these
    // cross-origin POSTs (navigationes.eu → api.navigationes.eu). The MAUI app sends
    // none and cannot run reCAPTCHA, so it passes; a script can pass the same way by
    // omitting Origin, which is why the per-IP rate limits stay the real protection
    // and this filter only turns away naive bots driving the web form.
    public class RecaptchaEndpointFilter(string action) : IEndpointFilter
    {
        public const string TokenHeader = "X-Recaptcha-Token";

        private readonly string _action = action;

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;

            if (http.Request.Headers.Origin.Count > 0)
            {
                var recaptcha = http.RequestServices.GetRequiredService<RecaptchaService>();
                var token = http.Request.Headers[TokenHeader].ToString();

                if (!await recaptcha.VerifyAsync(token, _action, http.Connection.RemoteIpAddress?.ToString()))
                {
                    // 200 + failure body, like every other refusal from these endpoints.
                    return Results.Json(new
                    {
                        isSuccess = false,
                        errorCode = ErrorCodes.CaptchaFailedError,
                        errorMessage = ErrorCodes.CaptchaFailedError
                    });
                }
            }

            return await next(context);
        }
    }
}
