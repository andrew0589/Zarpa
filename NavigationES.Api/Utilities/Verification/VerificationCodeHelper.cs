namespace NavigationES.Api.Utilities.Verification
{
    public static class VerificationCodeHelper
    {
        // Lifetime of an email verification code — also the figure the email quotes.
        public const int ExpiryMinutes = 15;

        // Wrong guesses allowed per code before it is burnt: 5 tries at 1 in 900 000
        // each, instead of an unlimited walk through the whole code space.
        public const int MaxAttempts = 5;

        /// <summary>
        /// Generates a numeric verification code and its expiry time.
        /// </summary>
        /// <param name="length">Number of digits (default = 6).</param>
        /// <param name="expiryMinutes">Minutes until expiration (default = <see cref="ExpiryMinutes"/>).</param>
        /// <returns>A tuple containing (code, expiry).</returns>
        public static (string Code, DateTime Expiry) Generate(int length = 6, int expiryMinutes = ExpiryMinutes)
        {
            if (length < 4 || length > 10)
                throw new ArgumentOutOfRangeException(nameof(length), "Length should be between 4 and 10 digits.");

            var min = (int)Math.Pow(10, length - 1);
            var max = (int)Math.Pow(10, length) - 1;

            // Using RandomNumberGenerator for security (better than Random)
            var randomNumber = System.Security.Cryptography.RandomNumberGenerator.GetInt32(min, max);
            var code = randomNumber.ToString();

            var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);
            return (code, expiry);
        }
    }
}
