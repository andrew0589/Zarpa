using Xunit;
using NavigationES.Api.Auth;
using NavigationES.Api.Services;

namespace NavigationES.Api.Tests
{
    // The pure parts of the admin/activity feature: how the X-Client header is filed
    // and how the inactivity email spells the deletion date.
    public class AdminUserTests
    {
        [Theory]
        [InlineData("web", "web")]
        [InlineData("Android", "android")]
        [InlineData(" ios ", "ios")]
        [InlineData("windows", "windows")]
        [InlineData("app", "app")]
        public void NormalizeClient_KeepsKnownClientsLowercased(string header, string expected)
        {
            Assert.Equal(expected, UserActivityMiddleware.NormalizeClient(header));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("curl/8.0")]
        [InlineData("web; drop table users")]
        public void NormalizeClient_FilesUnknownOrMissingUnderApp(string? header)
        {
            // Older app builds send no header at all; anything unexpected must not be
            // stored verbatim in the 20-char column.
            Assert.Equal(UserActivityMiddleware.UnknownClient, UserActivityMiddleware.NormalizeClient(header));
        }

        [Fact]
        public void FormatSpanishDate_SpellsTheMonthInSpanish()
        {
            Assert.Equal("12 de octubre de 2026", AdminUserService.FormatSpanishDate(new DateTime(2026, 10, 12)));
            Assert.Equal("1 de enero de 2027", AdminUserService.FormatSpanishDate(new DateTime(2027, 1, 1)));
            Assert.Equal("31 de diciembre de 2026", AdminUserService.FormatSpanishDate(new DateTime(2026, 12, 31)));
        }
    }
}
