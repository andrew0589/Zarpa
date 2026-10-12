using Xunit;
using NavigationES.Api.Services;
using static NavigationES.Api.Services.PublicSiteService;

namespace NavigationES.Api.Tests
{
    // The pure parts of the public pages: slugs (also hard-coded in index.html's
    // links), Spanish dates, which convocatoria still counts as "próxima", image
    // paths, and what the sitemap and a comunidad page contain.
    public class PublicSiteTests
    {
        private const string Origin = "https://navigationes.eu";

        [Theory]
        [InlineData("Cataluña", "cataluna")]
        [InlineData("Ciudad Autónoma de Ceuta", "ciudad-autonoma-de-ceuta")]
        [InlineData("País Vasco", "pais-vasco")]
        [InlineData("Región de Murcia", "region-de-murcia")]
        [InlineData("Reglamento (RIPA)", "reglamento-ripa")]
        [InlineData("Seguridad en la mar (PY)", "seguridad-en-la-mar-py")]
        public void Slug_StripsAccentsAndPunctuation(string name, string expected) =>
            Assert.Equal(expected, PublicSitePages.Slug(name));

        [Fact]
        public void FormatDate_IsSpanish() =>
            Assert.Equal("7 de noviembre de 2026", PublicSitePages.FormatDate(new DateOnly(2026, 11, 7)));

        [Fact]
        public void UpcomingDate_DropsPastSittings()
        {
            var today = new DateOnly(2026, 10, 12);
            Assert.Null(PublicSitePages.UpcomingDate(new DateOnly(2026, 10, 3), today));
            Assert.Equal(today, PublicSitePages.UpcomingDate(today, today));
            Assert.Equal(new DateOnly(2026, 11, 7), PublicSitePages.UpcomingDate(new DateOnly(2026, 11, 7), today));
            Assert.Null(PublicSitePages.UpcomingDate(null, today));
        }

        [Theory]
        [InlineData("images/questions/tema_5/x.png", "/img/questions/tema_5/x.png")]
        [InlineData("/questions/x.png", "/img/questions/x.png")]
        public void ImageUrl_MapsTheApiWwwrootPathUnderImg(string path, string expected) =>
            Assert.Equal(expected, PublicSitePages.ImageUrl(path));

        private static ComunidadSummary Cataluna(int exams = 2, DateOnly? next = null) =>
            new(3, "Cataluña", "cataluna", "https://agricultura.gencat.cat", next, exams, exams * 45,
                exams == 0 ? [] : [new LicenseSummary("PER", "Patrón de Embarcaciones de Recreo", exams, exams * 45, 2025, 2026, 10)]);

        [Fact]
        public void Sitemap_ListsEveryComunidadAndTopicOnTheSiteOrigin()
        {
            var topics = new List<TopicSummary> { new(5, 5, "Balizamiento", "balizamiento", 120, []) };
            var xml = PublicSitePages.Sitemap(Origin, [Cataluna()], topics);

            Assert.Contains("<loc>https://navigationes.eu/examenes</loc>", xml);
            Assert.Contains("<loc>https://navigationes.eu/examenes/cataluna</loc>", xml);
            Assert.Contains("<loc>https://navigationes.eu/temas/balizamiento</loc>", xml);
            Assert.Contains("<loc>https://navigationes.eu/aviso-legal.html</loc>", xml);
        }

        [Fact]
        public void Comunidad_ShowsTheNextSittingAndEscapesQuestionText()
        {
            var sample = new SampleExam("PER", "Patrón de Embarcaciones de Recreo", 2026, 10, "T1",
                [new SampleQuestion("¿Qué es <babor>?", ["Izquierda", "Derecha", "Proa", "Popa"], 1, null, null, null)]);
            var html = PublicSitePages.Comunidad(Origin, Cataluna(next: new DateOnly(2026, 11, 7)), sample, [Cataluna()], new DateOnly(2026, 10, 12));

            Assert.Contains("7 de noviembre de 2026", html);
            Assert.Contains("<link rel=\"canonical\" href=\"https://navigationes.eu/examenes/cataluna\">", html);
            Assert.Contains("&lt;babor&gt;", html);
            Assert.DoesNotContain("<babor>", html);
            Assert.Contains("Respuesta correcta: <strong>a) Izquierda</strong>", html);
            Assert.Contains("\"@type\":\"FAQPage\"", html);
        }

        [Fact]
        public void Comunidad_WithoutExams_SaysSoInsteadOfAnEmptyTable()
        {
            var html = PublicSitePages.Comunidad(Origin, Cataluna(exams: 0), null, [], new DateOnly(2026, 10, 12));

            Assert.Contains("no publica los cuestionarios", html);
            Assert.Contains("pendiente de publicar", html);
            Assert.DoesNotContain("<table>", html);
        }
    }
}
