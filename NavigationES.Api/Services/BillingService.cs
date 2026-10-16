using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NavigationES.Api.Data;
using NavigationES.Api.Data.Entities;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Dtos;
using Stripe;

namespace NavigationES.Api.Services
{
    // Subscriptions are sold on the website only (Stripe Checkout + Customer Portal);
    // the mobile apps never show prices — they just read BillingStatusDto. Access =
    // admin, OR the free trial is still running, OR a live Stripe subscription.
    //
    // Prices are found by lookup_key, never by price id: to change a price, create a
    // new Price in Stripe with the same lookup_key and "transfer lookup key" — no
    // deploy needed. Existing subscribers keep the price they signed up with.
    public class BillingService(NavigationESDbContext context, IConfiguration configuration, IMemoryCache cache, ILogger<BillingService> logger)
    {
        public const int TrialDays = 7;

        // past_due keeps access while Stripe retries the card (Smart Retries); it
        // ends up canceled or unpaid if every retry fails.
        private static readonly string[] LiveStatuses = ["active", "trialing", "past_due"];

        private static readonly string[] DefaultPlans = ["premium_monthly", "premium_quarterly", "premium_yearly"];
        private const string PlansCacheKey = "billing:plans";

        private readonly NavigationESDbContext _context = context;
        private readonly IConfiguration _configuration = configuration;
        private readonly IMemoryCache _cache = cache;
        private readonly ILogger<BillingService> _logger = logger;

        private string? SecretKey => Configured(_configuration["Stripe:SecretKey"]);
        public string? WebhookSecret => Configured(_configuration["Stripe:WebhookSecret"]);
        public bool IsConfigured => SecretKey is not null;

        private string[] PlanKeys =>
            _configuration.GetSection("Stripe:Plans").Get<string[]>() is { Length: > 0 } keys ? keys : DefaultPlans;

        private StripeClient Client => new(SecretKey);

        public async Task<bool> HasAccessAsync(long userId) =>
            (await GetStatusAsync(userId))?.HasAccess ?? false;

        public async Task<BillingStatusDto?> GetStatusAsync(long userId)
        {
            var user = await _context.Users.AsNoTracking()
                .Where(u => u.ID == userId)
                .Select(u => new { u.IsAdmin, u.TrialEndsAt, u.StripeCustomerId })
                .FirstOrDefaultAsync();
            if (user is null)
                return null;

            // The live subscription if there is one, otherwise the latest (for
            // "your subscription ended on ..." wording).
            var subscriptions = await _context.Subscriptions.AsNoTracking()
                .Where(s => s.UserID == userId)
                .OrderByDescending(s => s.UpdatedAt)
                .ToListAsync();
            var subscription = subscriptions.FirstOrDefault(s => LiveStatuses.Contains(s.Status))
                ?? subscriptions.FirstOrDefault();

            var source = ResolveAccess(user.IsAdmin, user.TrialEndsAt, subscription?.Status, DateTime.UtcNow);

            return new BillingStatusDto(
                source != BillingAccess.None,
                source,
                user.TrialEndsAt,
                subscription?.PriceLookupKey,
                subscription?.Status,
                subscription?.CurrentPeriodEnd,
                subscription?.CancelAtPeriodEnd ?? false,
                CanManage: user.StripeCustomerId is not null && IsConfigured);
        }

        // Which BillingAccess applies; a paying admin still shows as admin.
        public static string ResolveAccess(bool isAdmin, DateTime? trialEndsAt, string? subscriptionStatus, DateTime utcNow) =>
            isAdmin ? BillingAccess.Admin
            : subscriptionStatus is not null && LiveStatuses.Contains(subscriptionStatus) ? BillingAccess.Subscription
            : trialEndsAt > utcNow ? BillingAccess.Trial
            : BillingAccess.None;

        // The configured plans with their current Stripe prices, cached for a few
        // minutes so the public pricing page does not hit Stripe on every view.
        public async Task<List<BillingPlanDto>> GetPlansAsync()
        {
            if (!IsConfigured)
                return [];

            if (_cache.TryGetValue(PlansCacheKey, out List<BillingPlanDto>? cached) && cached is not null)
                return cached;

            var prices = await new PriceService(Client).ListAsync(new PriceListOptions
            {
                LookupKeys = [.. PlanKeys],
                Active = true,
                Expand = ["data.product"],
            });

            var plans = PlanKeys
                .Select(key => prices.Data.FirstOrDefault(p => p.LookupKey == key))
                .Where(p => p?.Recurring is not null && p.UnitAmount is not null)
                .Select(p => new BillingPlanDto(
                    p!.LookupKey,
                    p.Nickname ?? p.Product?.Name ?? p.LookupKey,
                    p.UnitAmount!.Value,
                    p.Currency,
                    IntervalMonths(p.Recurring)))
                .ToList();

            _cache.Set(PlansCacheKey, plans, TimeSpan.FromMinutes(10));
            return plans;
        }

        public async Task<ResultWithDataDto<RedirectUrlDto>> CreateCheckoutAsync(long userId, string lookupKey)
        {
            if (!IsConfigured)
                return ResultWithDataDto<RedirectUrlDto>.Failure(ErrorCodes.BillingUnavailableError);

            if (!PlanKeys.Contains(lookupKey))
                return ResultWithDataDto<RedirectUrlDto>.Failure(ErrorCodes.PlanNotFoundError);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.ID == userId);
            if (user is null)
                return ResultWithDataDto<RedirectUrlDto>.Failure(ErrorCodes.UserDoesNotExist);

            if (await _context.Subscriptions.AnyAsync(s => s.UserID == userId && LiveStatuses.Contains(s.Status)))
                return ResultWithDataDto<RedirectUrlDto>.Failure(ErrorCodes.AlreadySubscribedError);

            try
            {
                var prices = await new PriceService(Client).ListAsync(new PriceListOptions
                {
                    LookupKeys = [lookupKey],
                    Active = true,
                });
                var price = prices.Data.FirstOrDefault();
                if (price is null)
                    return ResultWithDataDto<RedirectUrlDto>.Failure(ErrorCodes.PlanNotFoundError);

                if (user.StripeCustomerId is null)
                {
                    var customer = await new CustomerService(Client).CreateAsync(new CustomerCreateOptions
                    {
                        Email = user.Email,
                        Name = user.Name,
                        Metadata = new() { ["userId"] = user.ID.ToString() },
                    });
                    user.StripeCustomerId = customer.Id;
                    await _context.SaveChangesAsync();
                }

                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    Mode = "subscription",
                    Customer = user.StripeCustomerId,
                    ClientReferenceId = user.ID.ToString(),
                    LineItems = [new() { Price = price.Id, Quantity = 1 }],
                    Locale = "es",
                    AllowPromotionCodes = true,
                    SuccessUrl = FrontendUrl("/suscripcion?checkout=ok"),
                    CancelUrl = FrontendUrl("/suscripcion"),
                    // Lets the webhook find the account even from subscription events alone.
                    SubscriptionData = new() { Metadata = new() { ["userId"] = user.ID.ToString() } },
                };

                // EU digital content: immediate access needs the buyer's explicit
                // consent to lose the 14-day withdrawal right. Stripe refuses the
                // checkbox unless the dashboard has a Terms of Service URL (Settings →
                // Business details), so it is tied to Stripe:TermsUrl: production sets
                // both, a dev sandbox without them still reaches Checkout.
                if (Configured(_configuration["Stripe:TermsUrl"]) is { } termsUrl)
                {
                    options.ConsentCollection = new() { TermsOfService = "required" };
                    options.CustomText = new()
                    {
                        TermsOfServiceAcceptance = new()
                        {
                            Message = $"Acepto los [Términos y condiciones]({termsUrl}) y solicito el acceso inmediato al contenido, " +
                                      "por lo que pierdo el derecho de desistimiento de 14 días.",
                        },
                    };
                }

                if (_configuration.GetValue<bool>("Stripe:AutomaticTax"))
                {
                    options.AutomaticTax = new() { Enabled = true };
                    options.CustomerUpdate = new() { Address = "auto", Name = "auto" };
                    options.BillingAddressCollection = "required";
                }

                var session = await new Stripe.Checkout.SessionService(Client).CreateAsync(options);
                return ResultWithDataDto<RedirectUrlDto>.Success(new RedirectUrlDto(session.Url));
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe checkout failed for user {UserId}", userId);
                return ResultWithDataDto<RedirectUrlDto>.Failure(ErrorCodes.BillingUnavailableError);
            }
        }

        // Stripe Customer Portal: cancel, switch plan, update the card, download invoices.
        public async Task<ResultWithDataDto<RedirectUrlDto>> CreatePortalAsync(long userId)
        {
            if (!IsConfigured)
                return ResultWithDataDto<RedirectUrlDto>.Failure(ErrorCodes.BillingUnavailableError);

            var customerId = await _context.Users.AsNoTracking()
                .Where(u => u.ID == userId)
                .Select(u => u.StripeCustomerId)
                .FirstOrDefaultAsync();
            if (customerId is null)
                return ResultWithDataDto<RedirectUrlDto>.Failure(ErrorCodes.BillingUnavailableError);

            try
            {
                var session = await new Stripe.BillingPortal.SessionService(Client).CreateAsync(new Stripe.BillingPortal.SessionCreateOptions
                {
                    Customer = customerId,
                    ReturnUrl = FrontendUrl("/suscripcion"),
                    Locale = "es",
                });
                return ResultWithDataDto<RedirectUrlDto>.Success(new RedirectUrlDto(session.Url));
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe portal failed for user {UserId}", userId);
                return ResultWithDataDto<RedirectUrlDto>.Failure(ErrorCodes.BillingUnavailableError);
            }
        }

        // Webhook entry point. Every relevant event just names a subscription; its
        // current state is re-read from Stripe, so duplicated or out-of-order
        // deliveries all converge to the same row.
        public async Task HandleEventAsync(Event stripeEvent)
        {
            string? subscriptionId = stripeEvent.Data.Object switch
            {
                Stripe.Checkout.Session { Mode: "subscription" } session => session.SubscriptionId,
                Subscription subscription => subscription.Id,
                _ => null,
            };

            if (subscriptionId is not null)
                await SyncSubscriptionAsync(subscriptionId);
        }

        private async Task SyncSubscriptionAsync(string subscriptionId)
        {
            var subscription = await new SubscriptionService(Client).GetAsync(subscriptionId);

            var row = await _context.Subscriptions.FirstOrDefaultAsync(s => s.StripeSubscriptionId == subscriptionId);
            if (row is null)
            {
                var userId = await FindUserIdAsync(subscription);
                if (userId is null)
                {
                    _logger.LogWarning("Stripe subscription {SubscriptionId} matches no account", subscriptionId);
                    return;
                }

                row = new SubscriptionEntity { UserID = userId.Value, StripeSubscriptionId = subscriptionId, Status = subscription.Status };
                _context.Subscriptions.Add(row);
            }

            var item = subscription.Items?.Data?.FirstOrDefault();
            row.Status = subscription.Status;
            row.PriceLookupKey = item?.Price?.LookupKey;
            row.CurrentPeriodEnd = item?.CurrentPeriodEnd;
            row.CancelAtPeriodEnd = subscription.CancelAtPeriodEnd || subscription.CancelAt is not null;
            row.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task<long?> FindUserIdAsync(Subscription subscription)
        {
            if (subscription.Metadata.TryGetValue("userId", out var value) && long.TryParse(value, out var id)
                && await _context.Users.AnyAsync(u => u.ID == id))
                return id;

            return await _context.Users
                .Where(u => u.StripeCustomerId == subscription.CustomerId)
                .Select(u => (long?)u.ID)
                .FirstOrDefaultAsync();
        }

        // Account deletion: stop billing right away. Best-effort, like the Apple
        // revoke — a Stripe failure is logged and must not keep the account alive.
        public async Task CancelAllAsync(long userId)
        {
            if (!IsConfigured)
                return;

            var live = await _context.Subscriptions.AsNoTracking()
                .Where(s => s.UserID == userId && LiveStatuses.Contains(s.Status))
                .Select(s => s.StripeSubscriptionId)
                .ToListAsync();

            foreach (var subscriptionId in live)
            {
                try
                {
                    await new SubscriptionService(Client).CancelAsync(subscriptionId);
                }
                catch (StripeException ex)
                {
                    _logger.LogError(ex, "Could not cancel Stripe subscription {SubscriptionId}", subscriptionId);
                }
            }
        }

        private string FrontendUrl(string path) =>
            (_configuration["AppSettings:FrontendUrl"] ?? string.Empty).TrimEnd('/') + path;

        public static int IntervalMonths(PriceRecurring recurring) => recurring.Interval switch
        {
            "year" => 12 * (int)recurring.IntervalCount,
            "month" => (int)recurring.IntervalCount,
            _ => 0,
        };

        private static string? Configured(string? value) =>
            string.IsNullOrWhiteSpace(value) || value == "TODO" ? null : value;
    }
}
