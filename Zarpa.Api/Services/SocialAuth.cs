namespace Zarpa.Api.Services
{
    // Bits shared by every social provider's OAuth flow.
    public static class SocialAuth
    {
        // Where the mobile app's WebAuthenticator listens for the final redirect.
        public const string AppCallbackUrl = "zarpa://auth-callback";
    }
}
