using CET96_ProjetoFinal.web.Services;
using CET96_ProjetoFinal.web.Settings; // Add this using statement
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

public class NewSmtpEmailSender : IEmailSender
{
    private readonly SmtpSettings _smtpSettings;

    // The settings are now injected safely and are strongly-typed.
    public NewSmtpEmailSender(IOptions<SmtpSettings> smtpSettings)
    {
        _smtpSettings = smtpSettings.Value;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var email = new MimeMessage();
        email.From.Add(new MailboxAddress("CondoManagerPrime", _smtpSettings.SenderEmail));
        email.To.Add(new MailboxAddress("", toEmail));
        email.Subject = subject;

        // Use a BodyBuilder to easily create an HTML email body.
        var builder = new BodyBuilder { HtmlBody = message };
        email.Body = builder.ToMessageBody();

        // The 'using' statement ensures the client is properly disposed of.
        using (var client = new SmtpClient())
        {
            // 1. Connect securely to the SMTP server. This is the part the old client failed at.
            await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, SecureSocketOptions.StartTls);

            // 2. Authenticate using your username and the 16-digit App Password.
            await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);

            // 3. Send the email.
            await client.SendAsync(email);

            // 4. Disconnect cleanly from the server.
            await client.DisconnectAsync(true);
        }
    }
}