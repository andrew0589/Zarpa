using NavigationES.Client.Resources.Languages;

namespace NavigationES.Client.Utilities
{
    // Durations and counts of the Estudiar screens (same wording as the web's StudyFormat).
    public static class StudyText
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

        public static string Videos(int count) =>
            count == 1 ? AppResources.StudyOneVideo : string.Format(AppResources.StudyVideosFormat, count);

        public static double Fraction(int part, int total) =>
            total <= 0 ? 0 : Math.Clamp((double)part / total, 0, 1);
    }
}
