using NavigationES.Api.Services;

namespace NavigationES.Api.Endpoints
{
    // The public, crawlable pages of navigationes.eu (see PublicSitePages). nginx on
    // the web container proxies these paths here, so they live on the site's origin
    // although the API answers them. Anonymous by design: they show only counts,
    // dates and a few sample questions.
    public static class PublicSiteEndpoints
    {
        // Content changes when exams are imported or a convocatoria date is moved,
        // a few times a month; an hour of caching keeps crawlers off the database.
        private const string CacheControl = "public, max-age=3600";

        public static IEndpointRouteBuilder MapPublicSiteEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/examenes", async (HttpContext http, PublicSiteService service, IConfiguration config) =>
            {
                var comunidades = await service.GetComunidadesAsync();
                var topics = await service.GetTopicsAsync();
                return Html(http, PublicSitePages.Hub(Origin(config), comunidades, topics, Today()));
            }).AllowAnonymous();

            app.MapGet("/examenes/{slug}", async (string slug, HttpContext http, PublicSiteService service, IConfiguration config) =>
            {
                var comunidades = await service.GetComunidadesAsync();
                var comunidad = comunidades.FirstOrDefault(c => c.Slug == slug.ToLowerInvariant());
                if (comunidad is null)
                    return NotFound(config);

                var sample = await service.GetSampleExamAsync(comunidad.Id);
                return Html(http, PublicSitePages.Comunidad(Origin(config), comunidad, sample, comunidades, Today()));
            }).AllowAnonymous();

            app.MapGet("/temas/{slug}", async (string slug, HttpContext http, PublicSiteService service, IConfiguration config) =>
            {
                var topics = await service.GetTopicsAsync();
                var topic = topics.FirstOrDefault(t => t.Slug == slug.ToLowerInvariant());
                if (topic is null)
                    return NotFound(config);

                var sample = await service.GetTopicSampleAsync(topic.Id);
                return Html(http, PublicSitePages.Topic(Origin(config), topic, sample, topics));
            }).AllowAnonymous();

            app.MapGet("/sitemap.xml", async (HttpContext http, PublicSiteService service, IConfiguration config) =>
            {
                var comunidades = await service.GetComunidadesAsync();
                var topics = await service.GetTopicsAsync();
                http.Response.Headers.CacheControl = CacheControl;
                return Results.Content(PublicSitePages.Sitemap(Origin(config), comunidades, topics), "application/xml; charset=utf-8");
            }).AllowAnonymous();

            return app;
        }

        // The site's public origin, never the API's: canonical URLs and the sitemap
        // must point at navigationes.eu, where nginx serves these same pages.
        private static string Origin(IConfiguration config) =>
            (config["AppSettings:FrontendUrl"] ?? "https://navigationes.eu").TrimEnd('/');

        // Spain's calendar decides whether a sitting is still "próximo".
        private static DateOnly Today()
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
            return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zone));
        }

        private static IResult Html(HttpContext http, string html)
        {
            http.Response.Headers.CacheControl = CacheControl;
            return Results.Content(html, "text/html; charset=utf-8");
        }

        private static IResult NotFound(IConfiguration config) =>
            Results.Content(PublicSitePages.NotFound(Origin(config)), "text/html; charset=utf-8", statusCode: StatusCodes.Status404NotFound);
    }
}
