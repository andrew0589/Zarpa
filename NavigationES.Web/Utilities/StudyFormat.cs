namespace NavigationES.Web.Utilities
{
    // Durations and media URLs of the Estudiar pages.
    public static class StudyFormat
    {
        // 4:48, 1:02:10.
        public static string Clock(int seconds)
        {
            var t = TimeSpan.FromSeconds(Math.Max(seconds, 0));
            return t.TotalHours >= 1 ? t.ToString(@"h\:mm\:ss") : t.ToString(@"m\:ss");
        }

        // 19 min (rounded, at least 1 for a non-empty total).
        public static string Minutes(int seconds) =>
            seconds <= 0 ? "0 min" : $"{Math.Max(1, (int)Math.Round(seconds / 60.0))} min";

        public static string Videos(int count) => count == 1 ? "1 vídeo" : $"{count} vídeos";

        public static int Percent(int part, int total) =>
            total <= 0 ? 0 : Math.Clamp((int)Math.Round(100.0 * part / total), 0, 100);

        // Relative media paths are served by the API (wwwroot/images/study, …).
        public static string? Media(ApiOptions api, string? url) =>
            string.IsNullOrWhiteSpace(url) ? null
            : Uri.IsWellFormedUriString(url, UriKind.Absolute) ? url
            : api.Url(url);
    }
}
