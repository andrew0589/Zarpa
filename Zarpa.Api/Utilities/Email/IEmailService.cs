namespace Zarpa.Api.Utilities.Email
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(EmailMessageModel emailMessage);
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isBodyHtml = true);
    }
}
