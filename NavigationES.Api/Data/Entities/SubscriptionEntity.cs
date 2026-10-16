using System.ComponentModel.DataAnnotations;

namespace NavigationES.Api.Data.Entities
{
    // Local mirror of one Stripe subscription, written only by the webhook
    // (BillingService.SyncSubscriptionAsync) so access checks never call Stripe.
    // Rows are kept after cancellation as history; access is decided by Status.
    public class SubscriptionEntity
    {
        [Key]
        public long ID { get; set; }

        public long UserID { get; set; }
        public UserEntity User { get; set; }

        [Required, MaxLength(100)]
        public string StripeSubscriptionId { get; set; }

        // Stripe status: active, trialing, past_due, unpaid, canceled, incomplete,
        // incomplete_expired, paused.
        [Required, MaxLength(30)]
        public string Status { get; set; }

        // The plan's lookup_key (premium_monthly/quarterly/yearly) — stable across
        // price changes, unlike the Stripe price id.
        [MaxLength(100)]
        public string? PriceLookupKey { get; set; }

        public DateTime? CurrentPeriodEnd { get; set; }

        // Cancelled from the Customer Portal: access lasts until CurrentPeriodEnd.
        public bool CancelAtPeriodEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
