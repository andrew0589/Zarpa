using Xunit;
using NavigationES.Api.Services;

namespace NavigationES.Api.Tests
{
    // The pure parts of the Estudiar section: which media URLs the admin may store,
    // and how the JSON columns read back (malformed = empty, never a 500).
    public class StudyTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("   ", null)]
        [InlineData("https://cdn.example.com/v/1.1.m3u8", "https://cdn.example.com/v/1.1.m3u8")]
        [InlineData("videos/1.1_casco.mp4", "videos/1.1_casco.mp4")]
        [InlineData("/images/study/tema01.webp", "images/study/tema01.webp")]
        public void NormalizeUrl_Accepts(string? raw, string? expected)
        {
            var (url, ok) = AdminStudyService.NormalizeUrl(raw);

            Assert.True(ok);
            Assert.Equal(expected, url);
        }

        [Theory]
        [InlineData("javascript:alert(1)")]
        [InlineData("ftp://example.com/v.mp4")]
        [InlineData("../appsettings.json")]
        public void NormalizeUrl_Refuses(string raw)
        {
            var (url, ok) = AdminStudyService.NormalizeUrl(raw);

            Assert.False(ok);
            Assert.Null(url);
        }

        [Fact]
        public void NormalizeUrl_RefusesTooLong() =>
            Assert.False(AdminStudyService.NormalizeUrl("videos/" + new string('a', 500)).Ok);

        [Fact]
        public void ParseChapters_ReadsTheExportFormat()
        {
            var chapters = StudyService.ParseChapters("""[{"title":"Introducción","start":0},{"title":"Quilla","start":21}]""");

            Assert.Equal(2, chapters.Count);
            Assert.Equal("Quilla", chapters[1].Title);
            Assert.Equal(21, chapters[1].Start);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("not json")]
        public void ParseJson_MalformedIsEmpty(string? json)
        {
            Assert.Empty(StudyService.ParseChapters(json));
            Assert.Empty(StudyService.ParseKeyTerms(json));
        }
    }
}
