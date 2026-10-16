using Refit;
using NavigationES.Shared.Dtos;

namespace NavigationES.ApiClient
{
    public interface IBillingApi
    {
        // Trial / subscription state of the signed-in account (web and apps).
        [Get("/api/billing/status")]
        Task<BillingStatusDto> GetStatusAsync();

        // The plans on sale with their current prices — website only.
        [Get("/api/billing/plans")]
        Task<List<BillingPlanDto>> GetPlansAsync();

        // Website only: the apps never start a purchase (store payment rules).
        [Post("/api/billing/checkout")]
        Task<ResultWithDataDto<RedirectUrlDto>> CreateCheckoutAsync(CheckoutRequestDto request);

        [Post("/api/billing/portal")]
        Task<ResultWithDataDto<RedirectUrlDto>> CreatePortalAsync();
    }
}
