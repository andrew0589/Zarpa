using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using static NavigationES.Api.Services.PublicSiteService;

namespace NavigationES.Api.Services
{
    // Server-rendered HTML for the public pages. The web app is Blazor WebAssembly
    // behind a login, so a crawler sees one paragraph of index.html; these pages are
    // what search engines index instead. nginx on navigationes.eu proxies /examenes,
    // /temas, /img and /sitemap.xml to the API, so every URL here is on the site's
    // own origin (`origin`, from AppSettings:FrontendUrl).
    //
    // Pure functions of their inputs (dates included), so they are unit-tested
    // without a database or a clock.
    public static class PublicSitePages
    {
        private static readonly string[] MonthNames =
            ["enero", "febrero", "marzo", "abril", "mayo", "junio",
             "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre"];

        // "Cataluña" -> "cataluna", "Reglamento (RIPA)" -> "reglamento-ripa".
        public static string Slug(string name)
        {
            var decomposed = name.ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                    continue;
                sb.Append(char.IsAsciiLetterOrDigit(ch) ? ch : '-');
            }
            var slug = sb.ToString();
            while (slug.Contains("--"))
                slug = slug.Replace("--", "-");
            return slug.Trim('-');
        }

        // "7 de noviembre de 2026".
        public static string FormatDate(DateOnly date) => $"{date.Day} de {MonthNames[date.Month - 1]} de {date.Year}";

        // "octubre de 2026"; "sin fecha" for an undated paper.
        public static string FormatSitting(int? year, int? month) =>
            year is null || month is null ? "sin fecha" : $"{MonthNames[month.Value - 1]} de {year}";

        // A date in the past is a sitting the administrator has not moved on yet,
        // not a "próximo examen".
        public static DateOnly? UpcomingDate(DateOnly? next, DateOnly today) => next is { } d && d >= today ? d : null;

        // Database paths are relative to the API's wwwroot ("images/questions/x.png");
        // the public pages serve them under /img on the site's origin.
        public static string ImageUrl(string path) =>
            "/img/" + (path.StartsWith("images/", StringComparison.OrdinalIgnoreCase) ? path["images/".Length..] : path.TrimStart('/'));

        private static string E(string? text) => WebUtility.HtmlEncode(text ?? "");

        // Comunidad de Madrid holds no exams of its own: its papers are the national
        // DGMM convocatoria, sat in Madrid. Worth saying, because that is what people
        // from any community without its own exams end up sitting.
        private static string? ExtraNote(string comunidadName) => comunidadName switch
        {
            "Comunidad de Madrid" => "Los exámenes de la Comunidad de Madrid son los de la convocatoria nacional de la "
                + "Dirección General de la Marina Mercante (DGMM), que se celebran en Madrid y a los que se presentan "
                + "también los aspirantes de las comunidades que no organizan exámenes propios.",
            _ => null,
        };

        public static string Hub(string origin, List<ComunidadSummary> comunidades, List<TopicSummary> topics, DateOnly today)
        {
            var totalExams = comunidades.Sum(c => c.ExamCount);
            var totalQuestions = comunidades.Sum(c => c.QuestionCount);
            var withExams = comunidades.Count(c => c.ExamCount > 0);

            var rows = new StringBuilder();
            foreach (var c in comunidades)
            {
                var next = UpcomingDate(c.NextExamDate, today);
                var licenses = c.Licenses.Count == 0 ? "—" : string.Join(", ", c.Licenses.Select(l => $"{E(l.Code)} ({l.ExamCount})"));
                rows.Append($"""
                    <tr>
                        <td><a href="/examenes/{c.Slug}">{E(c.Name)}</a></td>
                        <td>{(next is { } d ? FormatDate(d) : "Pendiente de publicar")}</td>
                        <td>{(c.ExamCount == 0 ? "—" : c.ExamCount.ToString(CultureInfo.InvariantCulture))}</td>
                        <td>{licenses}</td>
                    </tr>
                    """);
            }

            var faq = new (string Q, string A)[]
            {
                ("¿Cuándo son los próximos exámenes de PER y PNB?",
                    "Cada comunidad autónoma convoca sus propios exámenes. La tabla de esta página recoge la fecha de la próxima "
                    + "convocatoria de cada una, según el calendario oficial publicado por la comunidad."),
                ("¿Cuántas preguntas tiene el examen del PER?",
                    "45 preguntas tipo test en 90 minutos. Se aprueba con un máximo de 13 errores, con límites propios en "
                    + "Balizamiento (2), Reglamento RIPA (5) y Carta de navegación (2)."),
                ("¿Cuántas preguntas tiene el examen del PNB?",
                    "27 preguntas tipo test en 45 minutos, con un máximo de 10 errores."),
            };

            var body = $"""
                <h1>Exámenes náuticos por comunidad autónoma</h1>
                <p class="lead">Calendario de convocatorias y exámenes oficiales de PNB, PER, Patrón de Yate y Capitán de Yate.
                NavigationES reúne <strong>{totalExams} exámenes oficiales</strong> ({totalQuestions} preguntas) de {withExams} {(withExams == 1 ? "comunidad" : "comunidades")},
                tal como se publicaron, para que practiques con el examen real de tu comunidad.</p>

                <h2>Próximas convocatorias</h2>
                <div class="table-wrap"><table>
                    <thead><tr><th>Comunidad</th><th>Próximo examen</th><th>Exámenes oficiales</th><th>Titulaciones</th></tr></thead>
                    <tbody>{rows}</tbody>
                </table></div>
                <p class="muted">Las fechas proceden de los calendarios oficiales de cada comunidad; confirma siempre plazos y
                sedes en la web de la convocatoria.</p>

                {TopicLinks(topics)}

                <h2>Preguntas frecuentes</h2>
                {FaqHtml(faq)}

                {Cta("Practica con los exámenes oficiales de tu comunidad")}
                """;

            return Page(origin, "/examenes",
                "Exámenes PER, PNB y Patrón de Yate por comunidad: convocatorias " + today.Year,
                $"Calendario de exámenes náuticos {today.Year} por comunidad autónoma y {totalExams} exámenes oficiales de PNB, PER, Patrón y Capitán de Yate para practicar.",
                body,
                FaqJsonLd(faq));
        }

        public static string Comunidad(string origin, ComunidadSummary c, SampleExam? sample, List<ComunidadSummary> all, DateOnly today)
        {
            var next = UpcomingDate(c.NextExamDate, today);
            var licenseNames = c.Licenses.Count == 0 ? "PER, PNB y Patrón de Yate" : string.Join(", ", c.Licenses.Select(l => l.Code));

            var sb = new StringBuilder();
            sb.Append($"<h1>Exámenes de {E(licenseNames)} en {E(c.Name)}</h1>");

            sb.Append("<h2>Próxima convocatoria</h2>");
            sb.Append(next is { } d
                ? $"<p class=\"next\">El próximo examen teórico en {E(c.Name)} es el <strong>{FormatDate(d)}</strong>.</p>"
                : $"<p class=\"next\">La fecha del próximo examen en {E(c.Name)} está pendiente de publicar.</p>");
            if (c.ConvocatoriaUrl is not null)
                sb.Append($"<p>Plazos, inscripción y sedes: <a href=\"{E(c.ConvocatoriaUrl)}\" rel=\"noopener\">web oficial de la convocatoria</a>.</p>");

            if (ExtraNote(c.Name) is { } note)
                sb.Append($"<p>{E(note)}</p>");

            sb.Append("<h2>Exámenes oficiales disponibles</h2>");
            if (c.ExamCount == 0)
            {
                sb.Append($"""
                    <p>{E(c.Name)} no publica los cuestionarios de sus exámenes, así que todavía no hay exámenes oficiales de esta
                    comunidad en NavigationES. El temario es el mismo en toda España (Real Decreto 875/2014), por lo que puedes
                    prepararte con las preguntas por temas y con los exámenes oficiales de otras comunidades.</p>
                    """);
            }
            else
            {
                sb.Append($"<p>{c.ExamCount} exámenes oficiales de {E(c.Name)} ({c.QuestionCount} preguntas), transcritos de los documentos publicados por la comunidad, con su respuesta correcta:</p>");
                sb.Append("<div class=\"table-wrap\"><table><thead><tr><th>Titulación</th><th>Exámenes</th><th>Preguntas</th><th>Años</th><th>Último examen</th></tr></thead><tbody>");
                foreach (var l in c.Licenses)
                {
                    var years = l.FirstYear is null ? "—" : l.FirstYear == l.LastYear ? $"{l.FirstYear}" : $"{l.FirstYear}–{l.LastYear}";
                    sb.Append($"<tr><td>{E(l.Name)} ({E(l.Code)})</td><td>{l.ExamCount}</td><td>{l.QuestionCount}</td><td>{years}</td><td>{FormatSitting(l.LastYear, l.LastMonth)}</td></tr>");
                }
                sb.Append("</tbody></table></div>");
            }

            if (sample is { Questions.Count: > 0 })
            {
                var model = string.IsNullOrEmpty(sample.Model) ? "" : $", modelo {E(sample.Model)}";
                sb.Append($"<h2>Preguntas del examen de {E(sample.LicenseCode)} de {FormatSitting(sample.Year, sample.Month)}{model}</h2>");
                sb.Append($"<p>Algunas preguntas del último examen oficial de {E(sample.LicenseName)} en {E(c.Name)}. Pulsa en cada una para ver la respuesta correcta.</p>");
                sb.Append(QuestionsHtml(sample.Questions));
            }

            sb.Append(Cta(c.ExamCount > 0
                ? $"Haz los {c.ExamCount} exámenes oficiales de {c.Name} como en el examen real"
                : "Prepara el examen con preguntas por temas"));

            sb.Append("<h2>Otras comunidades</h2><ul class=\"links\">");
            foreach (var o in all.Where(o => o.Id != c.Id))
                sb.Append($"<li><a href=\"/examenes/{o.Slug}\">{E(o.Name)}</a></li>");
            sb.Append("</ul><p><a href=\"/examenes\">Calendario de todas las convocatorias</a></p>");

            var description = next is { } nd
                ? $"Próximo examen náutico en {c.Name}: {FormatDate(nd)}. "
                : $"Convocatorias de exámenes náuticos en {c.Name}. ";
            description += c.ExamCount > 0
                ? $"{c.ExamCount} exámenes oficiales de {licenseNames} con respuestas para practicar."
                : "Prepara el PER y el PNB con preguntas por temas.";

            var faq = new List<(string, string)>
            {
                ($"¿Cuándo es el próximo examen de PER en {c.Name}?",
                    next is { } fd ? $"El próximo examen teórico en {c.Name} es el {FormatDate(fd)}." : $"La fecha del próximo examen en {c.Name} está pendiente de publicar."),
            };
            if (c.ExamCount > 0)
                faq.Add(($"¿Dónde encontrar exámenes oficiales de {c.Name}?",
                    $"NavigationES tiene {c.ExamCount} exámenes oficiales de {c.Name} ({c.QuestionCount} preguntas) con su respuesta correcta, para hacerlos como simulacro."));

            return Page(origin, $"/examenes/{c.Slug}",
                $"Exámenes {licenseNames} {c.Name}: convocatoria {today.Year} y exámenes oficiales",
                description, sb.ToString(), FaqJsonLd(faq));
        }

        public static string Topic(string origin, TopicSummary t, List<SampleQuestion> sample, List<TopicSummary> all)
        {
            var sb = new StringBuilder();
            sb.Append($"<h1>Preguntas de {E(t.Name)}: test del examen náutico</h1>");
            sb.Append($"<p class=\"lead\">{E(t.Name)} es el tema {t.Number} del temario oficial. NavigationES tiene <strong>{t.QuestionCount} preguntas</strong> de este tema, cada una con su explicación.</p>");

            if (t.InExams.Count > 0)
            {
                sb.Append("<h2>Cuánto cuenta en el examen</h2><ul>");
                foreach (var (code, count, maxErrors) in t.InExams)
                    sb.Append($"<li><strong>{E(code)}</strong>: {count} preguntas{(maxErrors is { } m ? $", máximo {m} {(m == 1 ? "error" : "errores")}" : "")}.</li>");
                sb.Append("</ul>");
            }

            if (sample.Count > 0)
            {
                sb.Append($"<h2>Preguntas de ejemplo</h2><p>Pulsa en cada pregunta para ver la respuesta correcta y la explicación.</p>");
                sb.Append(QuestionsHtml(sample));
            }

            sb.Append(Cta($"Practica las {t.QuestionCount} preguntas de {t.Name}"));
            sb.Append(TopicLinks(all.Where(o => o.Id != t.Id).ToList(), "Otros temas"));
            sb.Append("<p><a href=\"/examenes\">Exámenes oficiales y convocatorias por comunidad</a></p>");

            return Page(origin, $"/temas/{t.Slug}",
                $"Test de {t.Name} — preguntas del examen PER y PNB",
                $"{t.QuestionCount} preguntas de {t.Name} con respuesta y explicación para preparar el examen de PNB, PER y Patrón de Yate.",
                sb.ToString(), null);
        }

        public static string Sitemap(string origin, List<ComunidadSummary> comunidades, List<TopicSummary> topics)
        {
            IEnumerable<string> paths =
            [
                "/", "/signup", "/examenes",
                .. comunidades.Select(c => $"/examenes/{c.Slug}"),
                .. topics.Select(t => $"/temas/{t.Slug}"),
                "/aviso-legal.html", "/privacidad.html", "/cookies.html", "/terminos.html",
            ];

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n");
            sb.Append("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">\n");
            foreach (var p in paths)
                sb.Append($"  <url><loc>{E(origin + p)}</loc></url>\n");
            sb.Append("</urlset>\n");
            return sb.ToString();
        }

        public static string NotFound(string origin) =>
            Page(origin, "/examenes", "Página no encontrada",
                "Página no encontrada.",
                "<h1>Página no encontrada</h1><p><a href=\"/examenes\">Ver los exámenes por comunidad</a></p>", null, index: false);

        private static string TopicLinks(List<TopicSummary> topics, string title = "Preguntas por temas")
        {
            if (topics.Count == 0)
                return "";
            var sb = new StringBuilder($"<h2>{E(title)}</h2><ul class=\"links\">");
            foreach (var t in topics)
                sb.Append($"<li><a href=\"/temas/{t.Slug}\">{E(t.Name)}</a></li>");
            return sb.Append("</ul>").ToString();
        }

        // Answers are hidden in <details> so the page works as a quiz without
        // JavaScript, while the text stays in the HTML for crawlers.
        private static string QuestionsHtml(List<SampleQuestion> questions)
        {
            var sb = new StringBuilder("<ol class=\"questions\">");
            foreach (var q in questions)
            {
                sb.Append("<li class=\"question\">");
                sb.Append($"<p class=\"q\">{E(q.Text)}</p>");
                if (q.ImageUrl is not null)
                    sb.Append($"<img src=\"{E(ImageUrl(q.ImageUrl))}\" alt=\"{E(Shorten(q.Text))}\" loading=\"lazy\">");
                sb.Append("<ol class=\"answers\" type=\"a\">");
                foreach (var a in q.Answers)
                    sb.Append($"<li>{E(a)}</li>");
                sb.Append("</ol>");
                var letter = q.CorrectIndex is >= 1 and <= 4 ? "abcd"[q.CorrectIndex - 1] : '?';
                var correct = q.CorrectIndex is >= 1 and <= 4 ? q.Answers[q.CorrectIndex - 1] : "";
                sb.Append($"<details><summary>Ver respuesta</summary><p class=\"correct\">Respuesta correcta: <strong>{letter}) {E(correct)}</strong></p>");
                // Explanations are HTML written by our own content pipeline, not user input.
                if (!string.IsNullOrWhiteSpace(q.Explanation))
                    sb.Append($"<div class=\"explanation\">{q.Explanation}</div>");
                if (q.ExplanationImageUrl is not null)
                    sb.Append($"<img src=\"{E(ImageUrl(q.ExplanationImageUrl))}\" alt=\"{E(Shorten(q.Text))}\" loading=\"lazy\">");
                sb.Append("</details></li>");
            }
            return sb.Append("</ol>").ToString();
        }

        private static string Shorten(string text) => text.Length <= 120 ? text : text[..117] + "…";

        private static string Cta(string text) => $"""
            <div class="cta">
                <p>{E(text)}</p>
                <a class="button" href="/signup">Crear cuenta</a>
            </div>
            """;

        private static string FaqHtml(IEnumerable<(string Q, string A)> faq) =>
            string.Concat(faq.Select(f => $"<h3>{E(f.Q)}</h3><p>{E(f.A)}</p>"));

        private static string FaqJsonLd(IEnumerable<(string Q, string A)> faq)
        {
            var data = new Dictionary<string, object>
            {
                ["@context"] = "https://schema.org",
                ["@type"] = "FAQPage",
                ["mainEntity"] = faq.Select(f => new Dictionary<string, object>
                {
                    ["@type"] = "Question",
                    ["name"] = f.Q,
                    ["acceptedAnswer"] = new Dictionary<string, object> { ["@type"] = "Answer", ["text"] = f.A },
                }).ToList(),
            };
            // The default encoder escapes <, > and & as <…, so the JSON cannot close the <script>.
            return $"<script type=\"application/ld+json\">{JsonSerializer.Serialize(data)}</script>";
        }

        private static string Page(string origin, string path, string title, string description, string body, string? jsonLd, bool index = true) => $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>{E(title)} | NavigationES</title>
                <meta name="description" content="{E(description)}">
                {(index ? "" : "<meta name=\"robots\" content=\"noindex\">")}
                <link rel="canonical" href="{E(origin + path)}">
                <meta property="og:type" content="website">
                <meta property="og:site_name" content="NavigationES">
                <meta property="og:title" content="{E(title)}">
                <meta property="og:description" content="{E(description)}">
                <meta property="og:url" content="{E(origin + path)}">
                <meta property="og:image" content="{E(origin)}/icon-192.png">
                <link rel="icon" href="/icon-192.png">
                {Style}
                {jsonLd}
            </head>
            <body>
                <header><a class="brand" href="/">NavigationES</a><nav><a href="/examenes">Exámenes</a><a href="/signin">Entrar</a></nav></header>
                <main>
                {body}
                </main>
                <footer>
                    <a href="/aviso-legal.html">Aviso legal</a> · <a href="/privacidad.html">Privacidad</a> ·
                    <a href="/cookies.html">Cookies</a> · <a href="/terminos.html">Términos</a>
                </footer>
            </body>
            </html>
            """;

        private const string Style = """
            <style>
                :root { --ink: #1F2933; --muted: #667085; --brand: #1b4965; --line: #E4E7EB; --soft: #F4F7FA; }
                * { box-sizing: border-box; }
                body { margin: 0; font-family: -apple-system, "Segoe UI", Roboto, Arial, sans-serif; color: var(--ink);
                       background: #FFFFFF; line-height: 1.6; }
                header, main, footer { max-width: 860px; margin: 0 auto; padding: 16px; }
                header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--line); }
                header nav a { margin-left: 16px; }
                .brand { font-weight: 700; font-size: 1.2em; text-decoration: none; }
                a { color: var(--brand); }
                h1 { font-size: 1.7em; line-height: 1.25; margin-top: 0.6em; }
                h2 { font-size: 1.25em; margin-top: 1.8em; }
                h3 { font-size: 1.05em; margin-bottom: 0.2em; }
                .lead { font-size: 1.1em; }
                .muted, footer { color: var(--muted); font-size: 0.9em; }
                footer { border-top: 1px solid var(--line); margin-top: 32px; }
                .table-wrap { overflow-x: auto; }
                table { border-collapse: collapse; width: 100%; font-size: 0.95em; }
                th, td { text-align: left; padding: 8px; border-bottom: 1px solid var(--line); vertical-align: top; }
                th { background: var(--soft); }
                .links { columns: 2; padding-left: 1.2em; }
                @media (max-width: 560px) { .links { columns: 1; } }
                .questions { padding-left: 1.4em; }
                .question { margin-bottom: 24px; }
                .q { font-weight: 600; margin-bottom: 4px; }
                .answers { margin-top: 0; }
                details { background: var(--soft); border-radius: 8px; padding: 8px 12px; }
                summary { cursor: pointer; color: var(--brand); font-weight: 600; }
                .correct { margin: 8px 0; }
                img { max-width: 100%; height: auto; display: block; margin: 8px 0; }
                .cta { margin: 32px 0; padding: 20px; border-radius: 12px; background: var(--soft); text-align: center; }
                .cta p { font-size: 1.1em; font-weight: 600; margin-top: 0; }
                .button { display: inline-block; padding: 10px 20px; border-radius: 8px; background: var(--brand); color: #FFFFFF;
                          text-decoration: none; font-weight: 600; }
            </style>
            """;
    }
}
