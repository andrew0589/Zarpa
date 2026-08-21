using NavigationES.Client.Resources.Languages;

namespace NavigationES.Client.Utilities
{
    public class BackendTranslator
    {
        public static string Translate(string? errorCode)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                return AppResources.UnknownError;

            var message = AppResources.ResourceManager.GetString(errorCode.Trim());
            return string.IsNullOrWhiteSpace(message)
                ? AppResources.UnknownError
                : message!;
        }
    };
}
