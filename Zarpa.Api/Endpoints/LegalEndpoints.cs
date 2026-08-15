namespace Zarpa.Api.Endpoints
{
    // Public legal pages linked from app-store listings and OAuth provider consoles
    // (Meta's Privacy Policy URL / Data Deletion Instructions URL, App Store, Play Store).
    public static class LegalEndpoints
    {
        public static IEndpointRouteBuilder MapLegalEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/privacy", () => Results.Content(PrivacyPolicyHtml, "text/html")).AllowAnonymous();
            app.MapGet("/data-deletion", () => Results.Content(DataDeletionHtml, "text/html")).AllowAnonymous();

            return app;
        }

        private const string PageStyle = """
            <style>
                body { font-family: -apple-system, "Segoe UI", Roboto, Arial, sans-serif;
                       max-width: 720px; margin: 0 auto; padding: 24px; line-height: 1.6;
                       color: #1F2933; background: #FFFFFF; }
                h1 { font-size: 1.6em; } h2 { font-size: 1.2em; margin-top: 1.6em; }
                a { color: #1b4965; }
                .muted { color: #667085; font-size: 0.9em; }
            </style>
            """;

        private const string PrivacyPolicyHtml = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Privacy Policy — Zarpa</title>
                {PageStyle}
            </head>
            <body>
                <h1>Privacy Policy</h1>
                <p class="muted">Zarpa &middot; Last updated: August 14, 2026</p>

                <p>Zarpa ("we", "us") is a mobile application that helps you prepare for the Spanish
                recreational boating exams (PNB, PER, Patrón de Yate and Capitán de Yate) with
                practice test exams. This page explains what personal data we collect, why, and what
                your rights are. For any privacy question, contact us at
                <a href="mailto:contact@zarpa.example">contact@zarpa.example</a>.</p>

                <h2>Data we collect</h2>
                <ul>
                    <li><strong>Account data</strong> — your name and email address. If you sign up with
                        a password, we store it only as a salted hash.</li>
                    <li><strong>Social sign-in</strong> — if you sign in with Google, Apple or Facebook,
                        we receive your name, email address and the provider's account identifier.
                        We never receive your password, contacts, friends or posts.</li>
                    <li><strong>Study data</strong> — your activity in the app: the exams you practice,
                        your answers, scores and progress statistics.</li>
                    <li><strong>Diagnostics</strong> — anonymous crash reports and usage telemetry that
                        help us keep the app working.</li>
                </ul>

                <h2>How we use your data</h2>
                <p>Only to provide and operate the service: signing you in, saving your practice
                results, tracking your progress and providing support. We do not sell personal data
                and we do not use it for advertising.</p>

                <h2>Where your data is stored</h2>
                <p>All data is stored on servers located in the European Union.</p>

                <h2>Who can see your data</h2>
                <ul>
                    <li>Only you — your practice results and progress are private to your account.</li>
                    <li>Our service providers, only as needed to run the service (hosting).</li>
                    <li>Authorities, only where the law requires it.</li>
                </ul>

                <h2>Retention and deletion</h2>
                <p>We keep your data while your account is active. You can permanently delete your
                account at any time — see the
                <a href="/data-deletion">data deletion instructions</a>.</p>

                <h2>Your rights</h2>
                <p>Under the GDPR you can request access to, correction of, deletion of, or a copy of
                your personal data, and you can object to its processing. Write to
                <a href="mailto:contact@zarpa.example">contact@zarpa.example</a> and we will
                respond within 30 days.</p>

                <h2>Children</h2>
                <p>Zarpa accounts are intended for users aged 13 and over.</p>

                <h2>Changes</h2>
                <p>If this policy changes, the new version will be published on this page.</p>
            </body>
            </html>
            """;

        private const string DataDeletionHtml = $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Data Deletion — Zarpa</title>
                {PageStyle}
            </head>
            <body>
                <h1>Data Deletion Instructions</h1>
                <p class="muted">Zarpa &middot; Last updated: August 14, 2026</p>

                <p>You can permanently delete your Zarpa account and the personal data
                associated with it in either of the following ways.</p>

                <h2>Option 1 — from the app</h2>
                <ol>
                    <li>Sign in to Zarpa.</li>
                    <li>Open the <strong>Settings</strong> tab.</li>
                    <li>Tap <strong>Delete account</strong> and confirm.</li>
                </ol>
                <p>This immediately and permanently deletes your account, your study data and your
                linked sign-in methods (Google, Apple or Facebook).</p>

                <h2>Option 2 — by email</h2>
                <p>Send an email to
                <a href="mailto:contact@zarpa.example">contact@zarpa.example</a> with the
                subject "Delete my account", from the email address your account is registered with.
                We will delete the account and confirm within 30 days.</p>

                <h2>If you signed in with Facebook</h2>
                <p>Deleting your account also deletes the data we received from Facebook (your name
                and email address). Additionally, you can remove Zarpa from
                <em>Settings &rarr; Apps and Websites</em> in your Facebook account.</p>

                <p>See also our <a href="/privacy">Privacy Policy</a>.</p>
            </body>
            </html>
            """;
    }
}
