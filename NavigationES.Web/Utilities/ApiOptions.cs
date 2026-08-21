namespace NavigationES.Web.Utilities
{
    // The resolved API base URL, injectable into components — social sign-in needs it
    // to build the full-page redirect to the API's /start endpoints.
    public record ApiOptions(string BaseUrl)
    {
        public string Url(string relativePath) => $"{BaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
    }
}
