namespace NavigationES.Api.Services
{
    // Bits shared by every social provider's OAuth flow.
    public static class SocialAuth
    {
        // Where the mobile app's WebAuthenticator listens for the final redirect.
        public const string AppCallbackUrl = "navigationes://auth-callback";

        // Which client started the flow — carried through the OAuth state so the
        // callback knows where to send the user back: the app's custom scheme or
        // the website's /auth-callback page.
        public const string AppClient = "app";
        public const string WebClient = "web";

        public static string NormalizeClient(string? client) =>
            string.Equals(client, WebClient, StringComparison.OrdinalIgnoreCase) ? WebClient : AppClient;
    }
}
