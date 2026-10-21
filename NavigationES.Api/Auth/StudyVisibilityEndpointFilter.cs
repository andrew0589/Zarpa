using Microsoft.EntityFrameworkCore;
using NavigationES.Api.Data;

namespace NavigationES.Api.Auth
{
    // Guards /api/study/*: while the videos are being produced the Estudiar section
    // is for administrators only. Setting Study:Public (env Study__Public=true) opens
    // it to every signed-in user — no client release needed, the clients ask
    // /api/study/access whether to show the tab. Everyone else gets 404, as if the
    // section did not exist yet.
    public class StudyVisibilityEndpointFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            return await IsOpenAsync(http) ? await next(context) : Results.NotFound();
        }

        public static async Task<bool> IsOpenAsync(HttpContext http)
        {
            if (http.RequestServices.GetRequiredService<IConfiguration>().GetValue<bool>("Study:Public"))
                return true;

            var userId = http.User.GetUserId();
            if (userId <= 0)
                return false;

            var db = http.RequestServices.GetRequiredService<NavigationESDbContext>();
            return await db.Users.AsNoTracking()
                .Where(u => u.ID == userId)
                .Select(u => u.IsAdmin)
                .FirstOrDefaultAsync();
        }
    }
}
