namespace Zarpa.Shared.Constants;

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
    #endregion
}
