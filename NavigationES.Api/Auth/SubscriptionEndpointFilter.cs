using NavigationES.Api.Services;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Auth
{
    // Guards the endpoints that START studying (topic practice, exam simulation):
    // the account needs the trial, a live subscription or admin. Answering and
    // finishing an already-open session stay free, so a trial that ends mid-exam
    // does not throw the user out. Read from the database on every call, like
    // AdminUserEndpointFilter, so a payment unlocks the app immediately.
    // Refusals are 402 + SubscriptionRequiredError; the clients show their paywall.
    public class SubscriptionEndpointFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            var billing = http.RequestServices.GetRequiredService<BillingService>();

            return await billing.HasAccessAsync(http.User.GetUserId())
                ? await next(context)
                : Results.Json(ResultDto.Failure(ErrorCodes.SubscriptionRequiredError), statusCode: StatusCodes.Status402PaymentRequired);
        }
    }
}
