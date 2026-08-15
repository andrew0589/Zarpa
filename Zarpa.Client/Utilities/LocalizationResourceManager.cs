using System.ComponentModel;
using System.Globalization;
using Zarpa.Client.Resources.Languages;

namespace Zarpa.Client.Utilities
{
    public class LocalizationResourceManager : INotifyPropertyChanged
    {
        public static LocalizationResourceManager Instance { get; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public LocalizationResourceManager()
        {
            // Automatically detect current culture
            SetCulture(CultureInfo.CurrentUICulture);
        }

        public string this[string text]
            => AppResources.ResourceManager.GetString(text, AppResources.Culture) ?? text;

        public void SetCulture(CultureInfo culture)
        {
            AppResources.Culture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        }
    }
}
