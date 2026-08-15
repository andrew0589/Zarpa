using System.Net;
using System.Net.Mail;

namespace Zarpa.Api.Utilities.Email
{
    // SMTP sender driven entirely by EmailSettings:* configuration. All defaults are
    // Zarpa placeholders — set real values (server, credentials) before going live.
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendEmailAsync(EmailMessageModel emailMessage)
        {
            try
            {
                if (emailMessage.To == null || !emailMessage.To.Any())
                {
                    _logger.LogError("Cannot send email: No recipients specified");
                    return false;
                }

                var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? "contact@zarpa.example";
                var senderName = emailMessage.CustomSenderName
                    ?? _configuration["EmailSettings:SenderName"]
                    ?? "Zarpa";
                var username = _configuration["EmailSettings:Username"] ?? senderEmail;
                var password = _configuration["EmailSettings:Password"] ?? string.Empty;
                var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

                using var smtp = new SmtpClient(smtpServer, smtpPort)
                {
                    EnableSsl = enableSsl,
                    Credentials = new NetworkCredential(username, password),
                    Timeout = 30000
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = emailMessage.Subject,
                    Body = emailMessage.Body,
                    IsBodyHtml = emailMessage.IsBodyHtml
                };

                foreach (var recipient in emailMessage.To)
                    mail.To.Add(recipient);

                if (emailMessage.Cc != null)
                    foreach (var cc in emailMessage.Cc)
                        mail.CC.Add(cc);

                if (emailMessage.Bcc != null)
                    foreach (var bcc in emailMessage.Bcc)
                        mail.Bcc.Add(bcc);

                if (!string.IsNullOrWhiteSpace(emailMessage.ReplyTo))
                    mail.ReplyToList.Add(emailMessage.ReplyTo);

                if (emailMessage.Attachments != null)
                {
                    foreach (var attachmentPath in emailMessage.Attachments)
                    {
                        if (File.Exists(attachmentPath))
                            mail.Attachments.Add(new Attachment(attachmentPath));
                        else
                            _logger.LogWarning($"Attachment not found: {attachmentPath}");
                    }
                }

                await smtp.SendMailAsync(mail);
                _logger.LogInformation($"Email sent successfully to {string.Join(", ", emailMessage.To)}");
                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError($"SMTP Error: {smtpEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isBodyHtml = true)
        {
            var emailMessage = new EmailMessageModel
            {
                To = new List<string> { to },
                Subject = subject,
                Body = body,
                IsBodyHtml = isBodyHtml
            };

            return await SendEmailAsync(emailMessage);
        }
    }
}
