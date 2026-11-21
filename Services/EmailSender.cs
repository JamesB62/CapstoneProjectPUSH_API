using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace CapstoneProjectPUSH_API.Services
{
    // Sends email through gmail -- if the configuration is missing, the email is written to the log instead
    public class EmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailSender> _logger;

        public EmailSender(IConfiguration configuration, ILogger<EmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // Wraps the sendasync method -- so a failed email doesn't cause a crash, logging the error instead
        public async Task<bool> TrySendAsync(string toEmail, string subject, string body)
        {
            try
            {
                await SendAsync(toEmail, subject, body);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email to {ToEmail} failed", toEmail);
                return false;
            }
        }

        // Sends the actual email
        private async Task SendAsync(string toEmail, string subject, string body)
        {
            // Email settings
            string senderName = GetSetting("Email:SenderName", "PUSH");
            string senderEmail = GetSetting("Email:SenderEmail", string.Empty);
            string appPassword = GetSetting("Email:AppPassword", string.Empty);
            string host = GetSetting("Email:Host", "smtp.gmail.com");
            string portText = GetSetting("Email:Port", "587");

            // Print to console if email hasn't been configured
            if (senderEmail.Length == 0 || appPassword.Length == 0)
            {
                _logger.LogWarning("[DEMO EMAIL] to={ToEmail} | {Subject} | {Body}", toEmail, subject, body);
                return;
            }

            int port;

            if (!int.TryParse(portText, out port))
            {
                port = 587;
            }

            // Builds the email
            MimeMessage message = new MimeMessage();
            message.From.Add(new MailboxAddress(senderName, senderEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using (SmtpClient client = new SmtpClient())
            {
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(senderEmail, appPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }

        // Helper
        // Gets email setting by key
        private string GetSetting(string key, string defaultValue)
        {
            string? value = _configuration[key];

            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            return value.Trim();
        }
    }
}