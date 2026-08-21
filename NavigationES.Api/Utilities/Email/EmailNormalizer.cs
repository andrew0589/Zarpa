namespace NavigationES.Api.Utilities.Email
{
    public static class EmailNormalizer
    {
        // Domains where Google ignores dots and "+tags" in the local part,
        // so a.lex@gmail.com, alex@gmail.com and alex+x@gmail.com are the same inbox.
        // Yahoo, Outlook etc. treat dots as significant, so they only get lowercasing.
        private static readonly HashSet<string> GmailDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "gmail.com",
            "googlemail.com"
        };

        /// <summary>
        /// Returns the canonical form of an email address, stored in Users.NormalizedEmail
        /// and used for all uniqueness checks and lookups. The original address is kept
        /// in Users.Email for display and for sending mail.
        /// </summary>
        public static string Normalize(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return string.Empty;

            var trimmed = email.Trim().ToLowerInvariant();

            var atIndex = trimmed.LastIndexOf('@');
            if (atIndex <= 0 || atIndex == trimmed.Length - 1)
                return trimmed;

            var local = trimmed[..atIndex];
            var domain = trimmed[(atIndex + 1)..];

            if (GmailDomains.Contains(domain))
            {
                var plusIndex = local.IndexOf('+');
                if (plusIndex >= 0)
                    local = local[..plusIndex];

                local = local.Replace(".", "");
                domain = "gmail.com"; // googlemail.com delivers to the same inbox
            }

            return $"{local}@{domain}";
        }
    }
}
