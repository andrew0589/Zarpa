using NavigationES.Client.Services.Environment;

namespace NavigationES.Client.Utilities
{
    public static class ImageUrls
    {
        // The DB stores relative paths ("images/questions/x.png"); the Image control
        // needs an absolute URL, whose base differs per environment (emulator/device).
        public static string? Absolute(IEnvironmentService environment, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
                return null;
            if (Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute))
                return imageUrl;

            return $"{environment.ApiBaseUrl.TrimEnd('/')}/{imageUrl.TrimStart('/')}";
        }
    }
}
