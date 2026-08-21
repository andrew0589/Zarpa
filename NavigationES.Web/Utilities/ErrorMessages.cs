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
            [ErrorCodes.UnknownError] = Unknown,
        };

        public static string Translate(string? errorCode) =>
            errorCode is not null && _messages.TryGetValue(errorCode.Trim(), out var message)
                ? message
                : Unknown;
    }
}
