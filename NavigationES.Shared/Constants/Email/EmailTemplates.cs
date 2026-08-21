namespace NavigationES.Shared.Constants.Email
{
    // NavigationES placeholder texts — adjust wording/branding before launch.
    public static class EmailTemplates
    {
        public static string BuildVerificationBody(string name, string code, int expiryMinutes)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border-radius: 10px;'>
                    <h2 style='color: #1b4965; text-align: center;'>Welcome aboard NavigationES, {name}!</h2>
                    <p style='font-size: 16px; line-height: 1.5;'>
                        You're one step away from starting your nautical exam preparation. To complete your registration, please verify your email address by entering the code below:
                    </p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='display: inline-block; font-size: 24px; font-weight: bold; color: #1b4965; letter-spacing: 3px;'>{code}</span>
                    </div>
                    <p style='font-size: 15px; line-height: 1.5;'>
                        This verification code will expire in <strong>{expiryMinutes} minutes</strong>, so please use it soon.
                    </p>
                    <p style='font-size: 15px; line-height: 1.5;'>
                        If you didn't request this email, you can safely ignore it.
                    </p>
                    <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'/>
                    <p style='font-size: 13px; color: #888; text-align: center;'>
                        Fair winds,<br/>
                        The NavigationES Team
                    </p>
                </div>";
        }

        // Sent once when an account is created through a social provider (Google/Apple/
        // Facebook). Those sign-ins skip the verification email — the provider already
        // vouched for the address — so this is the only onboarding email they receive.
        public static string BuildWelcomeBody(string name)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border-radius: 10px;'>
                    <h2 style='color: #1b4965; text-align: center;'>Welcome aboard NavigationES, {name}! ⚓</h2>
                    <p style='font-size: 16px; line-height: 1.5;'>
                        Your account is ready. We're glad to have you with us!
                    </p>
                    <p style='font-size: 15px; line-height: 1.5;'>
                        You can now practice test exams for your nautical qualifications — PNB, PER,
                        Patrón de Yate and Capitán de Yate — and track your progress along the way.
                    </p>
                    <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'/>
                    <p style='font-size: 13px; color: #888; text-align: center;'>
                        Fair winds,<br/>
                        The NavigationES Team
                    </p>
                </div>";
        }

        public static string BuildVerifiedBody(string name)
        {
            return $@"
                <div style='font-family: Arial, sans-serif; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border-radius: 10px;'>
                    <h2 style='color: #1b4965; text-align: center;'>Email Verified Successfully ⚓</h2>
                    <p style='font-size: 16px; line-height: 1.5;'>
                        Hi {name},<br/><br/>
                        Thank you for verifying your email address. Your account is now fully activated and ready to use!
                    </p>
                    <p style='font-size: 15px; line-height: 1.5;'>
                        You can now log in and start preparing for your nautical exams with NavigationES.
                    </p>
                    <hr style='border: none; border-top: 1px solid #ddd; margin: 30px 0;'/>
                    <p style='font-size: 13px; color: #888; text-align: center;'>
                        Fair winds,<br/>
                        The NavigationES Team
                    </p>
                </div>";
        }
    }
}
