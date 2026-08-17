namespace Zarpa.Client.Services
{
    // Which qualification (PNB/PER/PY/CY) the user is preparing for, chosen on the
    // Tests tab and persisted across app restarts. Sessions are created against it.
    public class SelectedLicenseService
    {
        private const string IdKey = "SelectedLicenseID";
        private const string CodeKey = "SelectedLicenseCode";

        public long? SelectedLicenseId
        {
            get
            {
                var value = Preferences.Get(IdKey, 0L);
                return value == 0 ? null : value;
            }
        }

        public string? SelectedLicenseCode => Preferences.Get(CodeKey, null);

        public void Select(long licenseId, string code)
        {
            Preferences.Set(IdKey, licenseId);
            Preferences.Set(CodeKey, code);
        }

        public void Clear()
        {
            Preferences.Remove(IdKey);
            Preferences.Remove(CodeKey);
        }
    }
}
