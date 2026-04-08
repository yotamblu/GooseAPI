using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace GooseAPI.Services
{
    /// <summary>
    /// Sends an admin notification when a new user registers. SMTP credentials live in <see cref="GmailNotificationSecrets"/> (gitignored).
    /// </summary>
    public sealed class NewUserRegistrationNotifier
    {
        private readonly ILogger<NewUserRegistrationNotifier> _logger;

        public NewUserRegistrationNotifier(ILogger<NewUserRegistrationNotifier> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Sends the notification before returning. Returns false if SMTP fails (user may already be persisted).
        /// </summary>
        public bool Notify(string fullName, string email, string userName)
        {
            fullName ??= "";
            email ??= "";
            userName ??= "";

            var password = GmailNotificationSecrets.AppPassword.Replace(" ", "", StringComparison.Ordinal);
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("GooseAPI", GmailNotificationSecrets.SenderEmail));
                message.To.Add(MailboxAddress.Parse(GmailNotificationSecrets.NotificationRecipientEmail));
                message.Subject = "New user registration";
                message.Body = new TextPart("plain")
                {
                    Text =
                        $"A new user registered on GooseAPI.\r\n\r\n" +
                        $"Full name: {fullName}\r\n" +
                        $"Email: {email}\r\n" +
                        $"Username: {userName}"
                };

                using var client = new SmtpClient();
                // Avoid TLS failures when the OS cannot reach CRL/OCSP (common on some networks).
                client.CheckCertificateRevocation = false;
                client.Connect(
                    GmailNotificationSecrets.SmtpHost,
                    GmailNotificationSecrets.SmtpPort,
                    SecureSocketOptions.StartTls);

                client.Authenticate(GmailNotificationSecrets.SenderEmail, password);
                client.Send(message);
                client.Disconnect(true);

                _logger.LogInformation(
                    "Registration notification email sent to {Recipient} for new user {UserName}.",
                    GmailNotificationSecrets.NotificationRecipientEmail,
                    userName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Registration email notification failed for user {UserName} ({Email}). Check Gmail app password, 2FA, and outbound port 587.",
                    userName,
                    email);
                return false;
            }
        }
    }
}
