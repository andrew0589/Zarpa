using Xunit;
using NavigationES.Api.Utilities.Verification;
using NavigationES.Shared.Constants;
using NavigationES.Shared.Validation;

namespace NavigationES.Api.Tests
{
    // The pure parts of the auth hardening: the shared password policy and the
    // verification code's lifetime.
    public class AuthHardeningTests
    {
        [Theory]
        [InlineData("a", ErrorCodes.PasswordTooShortError)]
        [InlineData("", ErrorCodes.PasswordTooShortError)]
        [InlineData(null, ErrorCodes.PasswordTooShortError)]
        [InlineData("abc de!", ErrorCodes.PasswordHasSpacesError)]
        [InlineData("123456!", ErrorCodes.PasswordMissingLetterError)]
        [InlineData("abcdef1", ErrorCodes.PasswordMissingSymbolError)]
        public void PasswordRules_ReportsTheFirstBrokenRule(string? password, string expected)
        {
            Assert.Equal(expected, PasswordRules.Validate(password));
        }

        [Theory]
        [InlineData("barco!")]
        [InlineData("Claude1790439600!Test")]
        [InlineData("x-y_z1")]
        public void PasswordRules_AcceptsLetterPlusSymbol(string password)
        {
            Assert.Null(PasswordRules.Validate(password));
        }

        [Fact]
        public void VerificationCode_IsSixDigitsAndLastsFifteenMinutes()
        {
            var before = DateTime.UtcNow;
            var (code, expiry) = VerificationCodeHelper.Generate();

            Assert.Equal(6, code.Length);
            Assert.True(code.All(char.IsAsciiDigit));
            Assert.InRange(expiry, before.AddMinutes(VerificationCodeHelper.ExpiryMinutes), DateTime.UtcNow.AddMinutes(VerificationCodeHelper.ExpiryMinutes));
            Assert.Equal(15, VerificationCodeHelper.ExpiryMinutes);
        }
    }
}
