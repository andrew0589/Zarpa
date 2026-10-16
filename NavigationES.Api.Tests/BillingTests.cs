using Xunit;
using NavigationES.Api.Services;
using NavigationES.Shared.Dtos;
using PriceRecurring = Stripe.PriceRecurring;

namespace NavigationES.Api.Tests
{
    // The pure parts of subscriptions: who gets access, and how a Stripe recurring
    // price turns into the "N months" the pricing page shows.
    public class BillingTests
    {
        private static readonly DateTime Now = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void ResolveAccess_TrialRunning_GrantsTrial()
        {
            Assert.Equal(BillingAccess.Trial, BillingService.ResolveAccess(false, Now.AddHours(1), null, Now));
        }

        [Fact]
        public void ResolveAccess_TrialOverAndNoSubscription_GrantsNothing()
        {
            Assert.Equal(BillingAccess.None, BillingService.ResolveAccess(false, Now.AddSeconds(-1), null, Now));
            Assert.Equal(BillingAccess.None, BillingService.ResolveAccess(false, null, null, Now));
        }

        [Theory]
        [InlineData("active")]
        [InlineData("trialing")]
        [InlineData("past_due")]
        public void ResolveAccess_LiveSubscription_GrantsSubscription(string status)
        {
            Assert.Equal(BillingAccess.Subscription, BillingService.ResolveAccess(false, Now.AddDays(-10), status, Now));
        }

        [Theory]
        [InlineData("canceled")]
        [InlineData("unpaid")]
        [InlineData("incomplete")]
        [InlineData("incomplete_expired")]
        [InlineData("paused")]
        public void ResolveAccess_DeadSubscription_FallsBackToTrial(string status)
        {
            Assert.Equal(BillingAccess.None, BillingService.ResolveAccess(false, Now.AddDays(-10), status, Now));
            // A checkout abandoned during the trial does not cut the trial short.
            Assert.Equal(BillingAccess.Trial, BillingService.ResolveAccess(false, Now.AddDays(1), status, Now));
        }

        [Fact]
        public void ResolveAccess_Admin_AlwaysHasAccess()
        {
            Assert.Equal(BillingAccess.Admin, BillingService.ResolveAccess(true, null, null, Now));
            Assert.Equal(BillingAccess.Admin, BillingService.ResolveAccess(true, null, "active", Now));
        }

        [Theory]
        [InlineData("month", 1, 1)]
        [InlineData("month", 3, 3)]
        [InlineData("year", 1, 12)]
        [InlineData("week", 1, 0)]
        public void IntervalMonths_ConvertsStripeRecurring(string interval, long count, int expected)
        {
            Assert.Equal(expected, BillingService.IntervalMonths(new PriceRecurring { Interval = interval, IntervalCount = count }));
        }
    }
}
