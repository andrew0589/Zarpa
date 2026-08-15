using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Maui.Core;

namespace Zarpa.Client.Utilities
{
    public static class UserMessageHelper
    {
        private static DateTime _suppressErrorsUntil = DateTime.MinValue;

        // Called when the session is force-closed on a 401: every in-flight call fails
        // at once, and their catch blocks would stack error snackbars on top of the
        // onboarding page the user just got redirected to.
        public static void SuppressErrors(TimeSpan window) =>
            _suppressErrorsUntil = DateTime.UtcNow.Add(window);

        public static async Task ShowSuccessAsync(string message)
        {
            await ShowSnackBarAsync("✅", Colors.Green, message);
        }

        public static async Task ShowWarningAsync(string message)
        {
            await ShowSnackBarAsync("⚠️", Colors.Orange, message);
        }

        public static async Task ShowErrorAsync(string message)
        {
            if (DateTime.UtcNow < _suppressErrorsUntil) return;

            await ShowSnackBarAsync("❌", Colors.Red, message);
        }

        private static async Task ShowSnackBarAsync(string emojiPrefix, Color backgroundColor, string message)
        {
            var snackbar = Snackbar.Make($"{emojiPrefix} {message}",
                duration: TimeSpan.FromSeconds(3),
                visualOptions: new SnackbarOptions
                {
                    BackgroundColor = backgroundColor,
                    TextColor = Colors.White,
                    CornerRadius = new CornerRadius(12),
                    Font = Microsoft.Maui.Font.Default
                });

            await snackbar.Show();
        }
    }
}
