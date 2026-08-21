namespace NavigationES.Api.Utilities.Email
{
    public class EmailMessageModel
    {
        public List<string> To { get; set; } = new();
        public List<string>? Cc { get; set; }
        public List<string>? Bcc { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsBodyHtml { get; set; } = true;
        public List<string>? Attachments { get; set; }
        public string? CustomSenderName { get; set; }
        public string? ReplyTo { get; set; }
    }
}
