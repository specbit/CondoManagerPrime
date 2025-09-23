using CET96_ProjetoFinal.web.Services; // Ensure this namespace is correct
using System.Net;
using System.Net.Mail;

/// <summary>
/// An implementation of <see cref="IEmailSender"/> that sends email using the built-in
/// <see cref="System.Net.Mail.SmtpClient"/>.
/// <para>
/// This class is configured by reading the "SmtpSettings" section from
/// <see cref="IConfiguration"/> (e.g., secrets.json) to connect to an SMTP provider like Gmail.
/// </para>
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="SmtpEmailSender"/> class.
    /// </summary>
    /// <param name="configuration">The application's configuration, used to retrieve SMTP settings.</param>
    public SmtpEmailSender(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Asynchronously sends an email using the SMTP settings from configuration.
    /// </summary>
    /// <param name="toEmail">The recipient's email address.</param>
    /// <param name="subject">The subject line of the email.</param>
    /// <param name="message">The HTML body content of the email.</param>
    /// <returns>A <see cref="Task"/> that represents the asynchronous send operation.</returns>
    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        // Read the settings from your secrets.json
        var smtpSettings = _configuration.GetSection("SmtpSettings");
        string host = smtpSettings["Host"];
        int port = int.Parse(smtpSettings["Port"]);
        bool enableSsl = bool.Parse(smtpSettings["EnableSsl"]);
        string username = smtpSettings["Username"];
        string password = smtpSettings["Password"];

        var client = new SmtpClient(host)
        {
            Port = port,
            EnableSsl = enableSsl,
            Credentials = new NetworkCredential(username, password)
        };

        var mailMessage = new MailMessage(
            username, // From
            toEmail,  // To
            subject,  // Subject
            message   // Body
        )
        {
            IsBodyHtml = true
        };

        // This sends the email
        await client.SendMailAsync(mailMessage);
    }
}