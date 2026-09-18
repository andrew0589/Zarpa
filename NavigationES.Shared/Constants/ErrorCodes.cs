namespace NavigationES.Shared.Constants;

public static class ErrorCodes
{
    #region Authentification
    public const string EmailAlreadyExistsError = "EmailAlreadyExistsError";
    public const string UserDoesNotExist = "UserDoesNotExistError";
    public const string IncorrectPasswordError = "IncorrectPasswordError";
    public const string UnknownError = "UnknownError";
    public const string UserBlocked = "UserBlocked";
    public const string EmailNotFoundError = "EmailNotFoundError";
    public const string ValidateYourEmail = "ValidateYourEmail";
    // Account has no password (created via Google/Apple/Facebook) but tried email+password signin.
    public const string UseSocialSigninError = "UseSocialSigninError";
    public const string GoogleAuthFailedError = "GoogleAuthFailedError";
    public const string AppleAuthFailedError = "AppleAuthFailedError";
    public const string FacebookAuthFailedError = "FacebookAuthFailedError";
    // Facebook account has no email we can link by (permission denied or phone-only account).
    public const string FacebookNoEmailError = "FacebookNoEmailError";
    // Profile rename: the new display name is empty or longer than the column allows (50).
    public const string NameNotValidError = "NameNotValidError";
    #endregion

    #region Password reset
    // The token in the emailed link is unknown or has already been used.
    public const string ResetLinkInvalidError = "ResetLinkInvalidError";
    // The token was valid but is past its one-hour lifetime.
    public const string ResetLinkExpiredError = "ResetLinkExpiredError";
    // Password validation: at least 6 characters, at least one letter, at least one symbol, no spaces.
    public const string PasswordTooShortError = "PasswordTooShortError";
    public const string PasswordHasSpacesError = "PasswordHasSpacesError";
    public const string PasswordMissingLetterError = "PasswordMissingLetterError";
    public const string PasswordMissingSymbolError = "PasswordMissingSymbolError";
    #endregion

    #region Admin (Usuarios tab)
    // Delete and the inactivity warning are refused for administrator accounts,
    // your own included — flip IsAdmin in the database first.
    public const string AdminAccountProtectedError = "AdminAccountProtectedError";
    // The SMTP send failed; nothing was recorded for the user.
    public const string EmailSendFailedError = "EmailSendFailedError";
    #endregion

    #region Admin (Convocatorias tab)
    // The comunidad id in the URL matches no seeded community.
    public const string ComunidadNotFoundError = "ComunidadNotFoundError";
    // The convocatoria site is not an absolute http(s) URL or exceeds the column (500).
    public const string ConvocatoriaUrlNotValidError = "ConvocatoriaUrlNotValidError";
    #endregion
}
