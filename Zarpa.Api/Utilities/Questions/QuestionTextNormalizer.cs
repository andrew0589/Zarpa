using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Zarpa.Api.Utilities.Questions
{
    // Canonical form of a question statement, used ONLY for duplicate detection
    // (Questions.ContentHash) — the display text is stored untouched. Two statements
    // that differ only in casing, accents, punctuation or spacing normalize the same.
    public static class QuestionTextNormalizer
    {
        public static string Normalize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            // FormD splits accented letters into base letter + combining mark,
            // so dropping NonSpacingMark turns á→a, é→e, ñ→n.
            var formD = text.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(formD.Length);
            var lastWasSpace = false;

            foreach (var ch in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;

                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                    lastWasSpace = false;
                }
                else if (!lastWasSpace && sb.Length > 0)
                {
                    // Any run of punctuation/whitespace collapses to a single space.
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }

            return sb.ToString().TrimEnd();
        }

        public static string ComputeHash(string text) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Normalize(text))));
    }
}
