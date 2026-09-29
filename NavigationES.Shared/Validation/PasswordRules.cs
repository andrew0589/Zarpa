using System.Text.RegularExpressions;
using NavigationES.Shared.Constants;

namespace NavigationES.Shared.Validation
{
    // The one password policy — the API enforces it on signup and reset, the web and
    // app forms check it first so the user sees the reason before a round-trip.
    // reset-password.html (plain JS) repeats these rules by hand.
    public static partial class PasswordRules
    {
        public const int MinLength = 6;

        // Returns the ErrorCodes value for the first rule the password breaks, or null.
        public static string? Validate(string? password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MinLength)
                return ErrorCodes.PasswordTooShortError;

            if (password.Contains(' '))
                return ErrorCodes.PasswordHasSpacesError;

            if (!LetterRegex().IsMatch(password))
                return ErrorCodes.PasswordMissingLetterError;

            if (!SymbolRegex().IsMatch(password))
                return ErrorCodes.PasswordMissingSymbolError;

            return null;
        }

        [GeneratedRegex("[a-zA-Z]")]
        private static partial Regex LetterRegex();

        [GeneratedRegex(@"[!@#$%^&*\-_=+\[\]{};:'"",.<>?/\\|`~]")]
        private static partial Regex SymbolRegex();
    }
}
