namespace NavigationES.Api.Endpoints
{
    // Public legal pages linked from app-store listings and OAuth provider consoles
    // (Meta's Privacy Policy URL / Data Deletion Instructions URL, App Store, Play Store).
    public static class LegalEndpoints
    {
        public static IEndpointRouteBuilder MapLegalEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/privacy", () => Results.Content(PrivacyPolicyHtml, "text/html")).AllowAnonymous();
            app.MapGet("/data-deletion", () => Results.Content(DataDeletionHtml, "text/html")).AllowAnonymous();
            // Linked from Stripe Checkout's consent checkbox (set the same URL as the
            // Terms of Service URL under Stripe Dashboard → Settings → Public details).
            app.MapGet("/terms", () => Results.Content(TermsHtml, "text/html")).AllowAnonymous();

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
                <title>Privacy Policy — NavigationES</title>
                {PageStyle}
            </head>
            <body>
                <h1>Privacy Policy</h1>
                <p class="muted">NavigationES &middot; Last updated: October 12, 2026</p>

                <p>NavigationES ("we", "us") is a mobile application that helps you prepare for the Spanish
                recreational boating exams (PNB, PER, Patrón de Yate and Capitán de Yate) with
                practice test exams. This page explains what personal data we collect, why, and what
                your rights are. For any privacy question, contact us at
                <a href="mailto:navigationes@gmail.com">navigationes@gmail.com</a>.</p>

                <h2>Data we collect</h2>
                <ul>
                    <li><strong>Account data</strong> — your name and email address. If you sign up with
                        a password, we store it only as a salted hash.</li>
                    <li><strong>Social sign-in</strong> — if you sign in with Google, Apple or Facebook,
                        we receive your name, email address and the provider's account identifier.
                        We never receive your password, contacts, friends or posts.</li>
                    <li><strong>Study data</strong> — your activity in the app: the exams you practice,
                        your answers, scores and progress statistics.</li>
                </ul>
                <p>We do not use analytics, advertising or tracking tools.</p>

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
                    <li>Google, when you sign up or request a password reset on the website: those
                        forms use Google reCAPTCHA to tell people from automated abuse, which sends
                        Google your IP address and information about your browser and how you use
                        the page. Google's <a href="https://policies.google.com/privacy">Privacy
                        Policy</a> and <a href="https://policies.google.com/terms">Terms of
                        Service</a> apply to that check. The mobile app does not use reCAPTCHA.</li>
                    <li>Stripe, which processes subscription payments made on the website. Your card
                        details go directly to Stripe and never reach our servers; we only keep the
                        subscription's status, plan and renewal date. Stripe's
                        <a href="https://stripe.com/privacy">Privacy Policy</a> applies.</li>
                    <li>Authorities, only where the law requires it.</li>
                </ul>

                <h2>Retention and deletion</h2>
                <p>We keep your data while your account is active. You can permanently delete your
                account at any time — see the
                <a href="/data-deletion">data deletion instructions</a>.</p>

                <h2>Your rights</h2>
                <p>Under the GDPR you can request access to, correction of, deletion of, or a copy of
                your personal data, and you can object to its processing. Write to
                <a href="mailto:navigationes@gmail.com">navigationes@gmail.com</a> and we will
                respond within 30 days.</p>

                <h2>Children</h2>
                <p>NavigationES accounts are intended for users aged 13 and over.</p>

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
                <title>Data Deletion — NavigationES</title>
                {PageStyle}
            </head>
            <body>
                <h1>Data Deletion Instructions</h1>
                <p class="muted">NavigationES &middot; Last updated: August 14, 2026</p>

                <p>You can permanently delete your NavigationES account and the personal data
                associated with it in either of the following ways.</p>

                <h2>Option 1 — from the app</h2>
                <ol>
                    <li>Sign in to NavigationES.</li>
                    <li>Open the <strong>Settings</strong> tab.</li>
                    <li>Tap <strong>Delete account</strong> and confirm.</li>
                </ol>
                <p>This immediately and permanently deletes your account, your study data and your
                linked sign-in methods (Google, Apple or Facebook), and cancels any active
                subscription so you are not charged again. Invoices already issued are kept by our
                payment provider for as long as tax law requires.</p>

                <h2>Option 2 — by email</h2>
                <p>Send an email to
                <a href="mailto:navigationes@gmail.com">navigationes@gmail.com</a> with the
                subject "Delete my account", from the email address your account is registered with.
                We will delete the account and confirm within 30 days.</p>

                <h2>If you signed in with Facebook</h2>
                <p>Deleting your account also deletes the data we received from Facebook (your name
                and email address). Additionally, you can remove NavigationES from
                <em>Settings &rarr; Apps and Websites</em> in your Facebook account.</p>

                <p>See also our <a href="/privacy">Privacy Policy</a>.</p>
            </body>
            </html>
            """;

        private const string TermsHtml = $"""
            <!DOCTYPE html>
            <html lang="es">
            <head>
                <meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Términos y condiciones — NavigationES</title>
                {PageStyle}
            </head>
            <body>
                <h1>Términos y condiciones</h1>
                <p class="muted">NavigationES &middot; Última actualización: 1 de octubre de 2026</p>

                <h2>El servicio</h2>
                <p>NavigationES ofrece tests de práctica y simulacros de examen para preparar las
                titulaciones náuticas de recreo (PNB, PER, Patrón de Yate y Capitán de Yate), en la web
                y en las aplicaciones móviles, con la misma cuenta.</p>

                <h2>Prueba gratuita</h2>
                <p>Cada cuenta nueva tiene acceso completo gratis durante 3 días, sin necesidad de
                tarjeta. Al terminar la prueba, practicar y hacer simulacros requiere una suscripción.</p>

                <h2>Suscripción</h2>
                <ul>
                    <li>La suscripción se contrata en la web y da acceso en todos los dispositivos.</li>
                    <li>Los planes disponibles (mensual, trimestral y anual) y sus precios, IVA incluido,
                        se muestran antes del pago.</li>
                    <li>Se renueva automáticamente al final de cada periodo y se cobra a la tarjeta
                        indicada, hasta que la canceles.</li>
                    <li>Puedes cancelarla en cualquier momento desde <em>Mi suscripción</em> en la web. Mantienes
                        el acceso hasta el final del periodo ya pagado; no se hacen reembolsos de periodos
                        parciales.</li>
                    <li>Si cambiamos el precio, te avisaremos por email antes de la siguiente renovación;
                        el nuevo precio se aplica solo a partir de esa renovación.</li>
                </ul>

                <h2>Derecho de desistimiento</h2>
                <p>Al contratar, solicitas el acceso inmediato al contenido digital y aceptas
                expresamente que, por ello, pierdes el derecho de desistimiento de 14 días previsto en
                el artículo 103.m) de la Ley General para la Defensa de los Consumidores y Usuarios.</p>

                <h2>Pagos</h2>
                <p>Los pagos los procesa Stripe. No guardamos los datos de tu tarjeta. Las facturas
                están disponibles en <em>Mi suscripción</em>.</p>

                <h2>Eliminación de la cuenta</h2>
                <p>Si eliminas tu cuenta, la suscripción se cancela en ese momento y no se vuelve a
                cobrar. Consulta las <a href="/data-deletion">instrucciones de eliminación</a> y la
                <a href="/privacy">política de privacidad</a>.</p>

                <h2>Contacto</h2>
                <p><a href="mailto:navigationes@gmail.com">navigationes@gmail.com</a></p>
            </body>
            </html>
            """;
    }
}
