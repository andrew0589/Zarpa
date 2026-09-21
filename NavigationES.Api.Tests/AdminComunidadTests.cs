using Xunit;
using NavigationES.Api.Services;
using NavigationES.Shared.Constants;
using static NavigationES.Api.Services.AdminComunidadService;

namespace NavigationES.Api.Tests
{
    // The pure parts behind the Convocatorias tab: which sitting counts as the last
    // one, which papers belong to it, how far each license lags behind, and what
    // the convocatoria site and the notes accept.
    public class AdminComunidadTests
    {
        private const long Baleares = 9;

        // Seeded license ids: PNB 1, PER 2, PY 3, CY 4.
        private static ExamPaper Paper(long licenseId, string license, int? year, int? month, string? model = null, string? file = null, int questions = 45) =>
            new(Baleares, licenseId, license, year, month, model, file, questions);

        // A third-party reprint: numbered, but nobody knows which sitting it was.
        private static ExamPaper Undated(long licenseId, string license, string model) =>
            Paper(licenseId, license, null, null, model);

        [Fact]
        public void Build_WithoutPapers_HasNoLastExam()
        {
            var row = Build(Baleares, "Islas Baleares", "https://caib.es", new DateOnly(2026, 10, 3), "Falta CY.", []);

            Assert.Equal(0, row.ExamCount);
            Assert.Null(row.LastExam);
            Assert.Empty(row.LastByLicense);
            Assert.Equal("https://caib.es", row.ConvocatoriaUrl);
            Assert.Equal(new DateOnly(2026, 10, 3), row.NextExamDate);
            Assert.Equal("Falta CY.", row.Notes);
        }

        [Fact]
        public void Build_PicksTheLatestSittingByYearThenMonth()
        {
            // December 2025 must lose to June 2026 although 12 > 6.
            var papers = new[]
            {
                Paper(2, "PER", 2025, 12, "A", "dec-a.pdf"),
                Paper(2, "PER", 2026, 6, "B", "jun-per-b.pdf", 44),
                Paper(1, "PNB", 2026, 6, "A", "jun-pnb-a.pdf", 27),
                Paper(2, "PER", 2026, 6, "A", "jun-per-a.pdf"),
            };

            var row = Build(Baleares, "Islas Baleares", null, null, null, papers);

            Assert.Equal(4, row.ExamCount);
            Assert.NotNull(row.LastExam);
            Assert.Equal(2026, row.LastExam.Year);
            Assert.Equal(6, row.LastExam.Month);

            // Only the June papers, in license order (PNB before PER) then by model.
            Assert.Equal(
                ["jun-pnb-a.pdf", "jun-per-a.pdf", "jun-per-b.pdf"],
                row.LastExam.Papers.Select(p => p.SourceFile).ToList());
            Assert.Equal(27, row.LastExam.Papers[0].QuestionCount);
            Assert.Equal("A", row.LastExam.Papers[1].Model);
        }

        [Fact]
        public void Build_ReportsEachLicensesOwnLatestSitting()
        {
            // CY stopped at December 2025 while PER already has June 2026 — the
            // per-license line is what shows the lag.
            var papers = new[]
            {
                Paper(4, "CY", 2025, 12, "A"),
                Paper(4, "CY", 2025, 12, "B"),
                Paper(4, "CY", 2025, 6, "A"),
                Paper(2, "PER", 2026, 6, "A"),
            };

            var row = Build(Baleares, "Islas Baleares", null, null, null, papers);

            Assert.Equal(2, row.LastByLicense.Count);
            Assert.Equal("PER", row.LastByLicense[0].LicenseCode);
            Assert.Equal((2026, 6, 1), (row.LastByLicense[0].Year, row.LastByLicense[0].Month, row.LastByLicense[0].ExamCount));
            Assert.Equal("CY", row.LastByLicense[1].LicenseCode);
            Assert.Equal((2025, 12, 2), (row.LastByLicense[1].Year, row.LastByLicense[1].Month, row.LastByLicense[1].ExamCount));

            // The community-level "last" is PER's June, and CY does not appear in it.
            Assert.Single(row.LastExam!.Papers);
            Assert.Equal("PER", row.LastExam.Papers[0].LicenseCode);
        }

        [Fact]
        public void Build_CountsUndatedPapersButNeverLetsThemBeTheLastSitting()
        {
            // The reprints are in the database and sittable, so they belong in the
            // count — but "último examen importado" has to stay June 2026.
            var papers = new[]
            {
                Paper(2, "PER", 2026, 6, "A", "jun-per-a.pdf"),
                Undated(2, "PER", "1"),
                Undated(2, "PER", "2"),
            };

            var row = Build(Baleares, "Islas Baleares", null, null, null, papers);

            Assert.Equal(3, row.ExamCount);
            Assert.Equal((2026, 6), (row.LastExam!.Year, row.LastExam.Month));
            Assert.Single(row.LastExam.Papers);
            Assert.Equal("jun-per-a.pdf", row.LastExam.Papers[0].SourceFile);
            Assert.Equal((2026, 6, 1), (row.LastByLicense[0].Year, row.LastByLicense[0].Month, row.LastByLicense[0].ExamCount));
        }

        [Fact]
        public void Build_WithOnlyUndatedPapers_ReportsTheCountAndNoLastSitting()
        {
            var row = Build(Baleares, "Islas Baleares", null, null, null, [Undated(2, "PER", "1"), Undated(2, "PER", "2")]);

            Assert.Equal(2, row.ExamCount);
            Assert.Null(row.LastExam);
            Assert.Empty(row.LastByLicense);
        }

        [Fact]
        public void Build_PassesTheHandKeptFieldsThroughUnchanged()
        {
            var notes = "Plantilla publicada;\nfaltan los enunciados de CY.";
            var row = Build(Baleares, "Islas Baleares", "https://www.caib.es/sites/transportmaritim/es/", new DateOnly(2026, 12, 12), notes, [Paper(2, "PER", 2026, 6)]);

            Assert.Equal("https://www.caib.es/sites/transportmaritim/es/", row.ConvocatoriaUrl);
            Assert.Equal(new DateOnly(2026, 12, 12), row.NextExamDate);
            Assert.Equal(notes, row.Notes);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void NormalizeUrl_TreatsBlankAsNoLink(string? raw)
        {
            Assert.Equal((null, null), NormalizeUrl(raw));
        }

        [Fact]
        public void NormalizeUrl_TrimsAndKeepsAbsoluteHttpUrls()
        {
            Assert.Equal(("https://www.caib.es/sites/transportmaritim/es/", null), NormalizeUrl("  https://www.caib.es/sites/transportmaritim/es/  "));
            Assert.Equal(("http://nautica.gencat.cat", null), NormalizeUrl("http://nautica.gencat.cat"));
        }

        [Theory]
        [InlineData("caib.es")]                    // no scheme
        [InlineData("ftp://caib.es/examenes")]     // not http(s)
        [InlineData("javascript:alert(1)")]        // would run in the link
        [InlineData("https://")]                   // no host
        public void NormalizeUrl_RejectsAnythingButHttpLinks(string raw)
        {
            Assert.Equal((null, ErrorCodes.ConvocatoriaUrlNotValidError), NormalizeUrl(raw));
        }

        [Fact]
        public void NormalizeUrl_RejectsUrlsLongerThanTheColumn()
        {
            var tooLong = "https://caib.es/" + new string('a', UrlMaxLength);
            Assert.Equal((null, ErrorCodes.ConvocatoriaUrlNotValidError), NormalizeUrl(tooLong));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("\n\n")]
        public void NormalizeNotes_TreatsBlankAsNothingWritten(string? raw)
        {
            Assert.Equal((null, null), NormalizeNotes(raw));
        }

        [Fact]
        public void NormalizeNotes_TrimsTheEndsAndKeepsTheLineBreaksInside()
        {
            // What the administrator typed as separate lines has to come back as
            // separate lines; only the stray whitespace around it goes.
            Assert.Equal(
                ("Plantilla publicada.\n\nFaltan los enunciados de CY:\n- pedirlos por email.", null),
                NormalizeNotes("  Plantilla publicada.\n\nFaltan los enunciados de CY:\n- pedirlos por email.\n "));
        }

        [Fact]
        public void NormalizeNotes_RejectsNotesLongerThanTheColumn()
        {
            Assert.Equal((new string('a', NotesMaxLength), null), NormalizeNotes(new string('a', NotesMaxLength)));
            Assert.Equal((null, ErrorCodes.ConvocatoriaNotesTooLongError), NormalizeNotes(new string('a', NotesMaxLength + 1)));
        }
    }
}
