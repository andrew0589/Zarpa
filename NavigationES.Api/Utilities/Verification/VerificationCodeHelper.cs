namespace NavigationES.Api.Utilities.Verification
{
    public static class VerificationCodeHelper
    {
        /// <summary>
        /// Generates a numeric verification code and its expiry time.
        /// </summary>
        /// <param name="length">Number of digits (default = 6).</param>
        /// <param name="expiryHours">Hours until expiration (default = 1 hour).</param>
        /// <returns>A tuple containing (code, expiry).</returns>
        public static (string Code, DateTime Expiry) Generate(int length = 6, int expiryHours = 1)
        {
            if (length < 4 || length > 10)
                throw new ArgumentOutOfRangeException(nameof(length), "Length should be between 4 and 10 digits.");

            var min = (int)Math.Pow(10, length - 1);
            var max = (int)Math.Pow(10, length) - 1;

            // Using RandomNumberGenerator for security (better than Random)
            var randomNumber = System.Security.Cryptography.RandomNumberGenerator.GetInt32(min, max);
            var code = randomNumber.ToString();

            var expiry = DateTime.UtcNow.AddHours(expiryHours);
            return (code, expiry);
        }
    }
}
