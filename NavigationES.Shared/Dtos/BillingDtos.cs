namespace NavigationES.Shared.Dtos
{
    // What the signed-in account may use right now. AccessSource is one of
    // BillingAccess.* — the clients pick their wording from it. The plan details are
    // those of the account's current (or latest) subscription, null when there is none.
    public record BillingStatusDto(
        bool HasAccess,
        string AccessSource,
        DateTime? TrialEndsAt,
        string? PlanLookupKey,
        string? SubscriptionStatus,
        DateTime? CurrentPeriodEnd,
        bool CancelAtPeriodEnd,
        bool CanManage);

    // One purchasable plan as configured in Stripe. Amount is in cents, VAT included.
    public record BillingPlanDto(string LookupKey, string Name, long AmountCents, string Currency, int IntervalMonths);

    public record CheckoutRequestDto(string LookupKey);

    // Stripe-hosted page (Checkout or Customer Portal) the browser is sent to.
    public record RedirectUrlDto(string Url);

    public static class BillingAccess
    {
        public const string Admin = "admin";
        public const string Trial = "trial";
        public const string Subscription = "subscription";
        public const string None = "none";
    }
}
