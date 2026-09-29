using NavigationES.Shared.Constants;

namespace NavigationES.Web.Utilities
{
    // Web counterpart of the MAUI BackendTranslator: API error codes → the same
    // Spanish messages the app shows (kept in sync with AppResources.resx by hand).
    public static class ErrorMessages
    {
        private const string Unknown = "Ha ocurrido un error desconocido. Por favor, inténtalo de nuevo.";

        private static readonly Dictionary<string, string> _messages = new()
        {
            [ErrorCodes.UserDoesNotExist] = "¡El usuario no existe!",
            [ErrorCodes.IncorrectPasswordError] = "¡Contraseña incorrecta!",
            [ErrorCodes.UserBlocked] = "Tu cuenta ha sido bloqueada por un administrador.",
            [ErrorCodes.UseSocialSigninError] = "Esta cuenta se creó con Google, Apple o Facebook. Usa el botón correspondiente para iniciar sesión.",
            [ErrorCodes.ValidateYourEmail] = "Valida tu correo electrónico",
            [ErrorCodes.EmailAlreadyExistsError] = "¡El correo electrónico ya existe!",
            [ErrorCodes.EmailNotFoundError] = "¡Correo electrónico no encontrado!",
            [ErrorCodes.GoogleAuthFailedError] = "Error al iniciar sesión con Google. Inténtalo de nuevo.",
            [ErrorCodes.AppleAuthFailedError] = "Error al iniciar sesión con Apple. Inténtalo de nuevo.",
            [ErrorCodes.FacebookAuthFailedError] = "Error al iniciar sesión con Facebook. Inténtalo de nuevo.",
            [ErrorCodes.FacebookNoEmailError] = "Tu cuenta de Facebook no tiene una dirección de correo que podamos usar. Regístrate con tu correo electrónico.",
            [ErrorCodes.NameNotValidError] = "El nombre no es válido (máximo 50 caracteres).",
            [ErrorCodes.EmailNotValidError] = "¡Por favor, agrega un correo electrónico válido!",
            [ErrorCodes.PasswordTooShortError] = "La contraseña es demasiado corta (mínimo 6 caracteres).",
            [ErrorCodes.PasswordHasSpacesError] = "La contraseña no debe contener espacios.",
            [ErrorCodes.PasswordMissingLetterError] = "La contraseña debe contener al menos una letra.",
            [ErrorCodes.PasswordMissingSymbolError] = "La contraseña debe contener al menos un símbolo (!@#$%^&*…).",
            [ErrorCodes.TooManyRequestsError] = "Demasiados intentos. Espera unos minutos y vuelve a intentarlo.",
            [ErrorCodes.CaptchaFailedError] = "No hemos podido comprobar que no eres un robot. Recarga la página e inténtalo de nuevo.",
            [ErrorCodes.VerificationCodeInvalidError] = "Código no válido. Revisa los datos e inténtalo de nuevo.",
            [ErrorCodes.VerificationCodeExpiredError] = "El código ha caducado. Regístrate de nuevo para recibir uno nuevo.",
            [ErrorCodes.VerificationAttemptsExceededError] = "Demasiados códigos incorrectos. Regístrate de nuevo para recibir un código nuevo.",
            [ErrorCodes.EmailAlreadyVerifiedError] = "Tu correo electrónico ya está validado.",
            [ErrorCodes.AdminAccountProtectedError] = "Esta acción no está disponible para cuentas de administrador.",
            [ErrorCodes.EmailSendFailedError] = "No se ha podido enviar el correo. Inténtalo de nuevo más tarde.",
            [ErrorCodes.ComunidadNotFoundError] = "La comunidad autónoma no existe.",
            [ErrorCodes.ConvocatoriaUrlNotValidError] = "La dirección web no es válida: debe empezar por http:// o https:// (máximo 500 caracteres).",
            [ErrorCodes.ConvocatoriaNotesTooLongError] = "Las notas son demasiado largas (máximo 4000 caracteres).",
            [ErrorCodes.UnknownError] = Unknown,
        };

        public static string Translate(string? errorCode) =>
            errorCode is not null && _messages.TryGetValue(errorCode.Trim(), out var message)
                ? message
                : Unknown;

        // For catch blocks around API calls: the auth endpoints' rate limit answers
        // HTTP 429, which Refit raises as an exception rather than a result.
        public static string FromException(Exception ex) =>
            ex is Refit.ApiException { StatusCode: System.Net.HttpStatusCode.TooManyRequests }
                ? Translate(ErrorCodes.TooManyRequestsError)
                : Unknown;
    }
}
