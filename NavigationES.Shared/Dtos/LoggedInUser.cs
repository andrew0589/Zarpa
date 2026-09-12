namespace NavigationES.Shared.Dtos
{
    // IsAdmin has a default so sessions serialized before the flag existed (web
    // localStorage, app Preferences) still deserialize. It only drives what the
    // clients SHOW (the Usuarios tab) — the API re-checks the database on every
    // admin call, so a forged flag buys nothing.
    public record LoggedInUser(long Id, string Name, string Email, bool IsEmailVerified, bool IsAdmin = false);
}
