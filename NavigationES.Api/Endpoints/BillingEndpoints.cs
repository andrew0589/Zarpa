using System.Security.Claims;
using NavigationES.Api.Auth;
using NavigationES.Api.Services;
using NavigationES.Shared.Dtos;

namespace NavigationES.Api.Endpoints
{
    public static class BillingEndpoints
    {
        public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
        {
            // Every client (web and apps) asks this after sign-in and on resume.
            app.MapGet("/api/billing/status", async (ClaimsPrincipal user, BillingService billing) =>
                await billing.GetStatusAsync(user.GetUserId()) is { } status
                    ? Results.Ok(status)
                    : Results.Unauthorized());

            // Public: the website's pricing section is visible before signing in.
            app.MapGet("/api/billing/plans", async (BillingService billing) =>
                TypedResults.Ok(await billing.GetPlansAsync()))
                .AllowAnonymous();

            // Website only — the apps never offer a purchase (store payment rules).
            app.MapPost("/api/billing/checkout", async (CheckoutRequestDto request, ClaimsPrincipal user, BillingService billing) =>
                TypedResults.Ok(await billing.CreateCheckoutAsync(user.GetUserId(), request.LookupKey)));

            app.MapPost("/api/billing/portal", async (ClaimsPrincipal user, BillingService billing) =>
                TypedResults.Ok(await billing.CreatePortalAsync(user.GetUserId())));

            // Stripe → API. Anonymous, authenticated instead by the Stripe-Signature
            // header; 404 while no webhook secret is configured (fails closed).
            app.MapPost("/api/billing/webhook", async (HttpRequest request, BillingService billing, ILogger<BillingService> logger) =>
            {
                if (billing.WebhookSecret is not { } secret || !billing.IsConfigured)
                    return Results.NotFound();

                // Stripe.net throws a NullReferenceException (not a StripeException) on a
                // missing header, so an unsigned request is turned away here.
                if (string.IsNullOrEmpty(request.Headers["Stripe-Signature"]))
                    return Results.BadRequest();

                var json = await new StreamReader(request.Body).ReadToEndAsync();
                Stripe.Event stripeEvent;
                try
                {
                    stripeEvent = Stripe.EventUtility.ConstructEvent(json, request.Headers["Stripe-Signature"], secret, throwOnApiVersionMismatch: false);
                }
                catch (Stripe.StripeException ex)
                {
                    logger.LogWarning(ex, "Rejected Stripe webhook");
                    return Results.BadRequest();
                }

                await billing.HandleEventAsync(stripeEvent);
                return Results.Ok();
            }).AllowAnonymous();

            return app;
        }
    }
}
