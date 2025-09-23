using SendGrid;
using SendGrid.Helpers.Mail;

namespace CET96_ProjetoFinal.web.Services
{
    /// <summary>
    /// An implementation of <see cref="IEmailSender"/> that uses the official SendGrid API.
    /// <para>
    /// This class uses the modern SendGrid NuGet package, which handles all SMTP
    /// settings (Host, Port, etc.) internally. It only requires a single
    /// API key ("SendGridKey"), which is loaded from User Secrets for security.
    /// </para>
    /// <para>
    /// This is why appsettings.json does not contain a large "SmtpSettings" block
    /// like older System.Net.Mail.SmtpClient implementations.
    /// </para>
    /// </summary>
    public class SendGridEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SendGridEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Sends an email asynchronously using the SendGrid email service.
        /// </summary>
        /// <remarks>This method requires a valid SendGrid API key to be configured in the application
        /// settings under the key "SendGridKey". If the API key is missing or invalid, an exception will be thrown.
        /// Ensure that the sender's email address is verified in your SendGrid account before using this
        /// method.</remarks>
        /// <param name="toEmail">The recipient's email address. This must be a valid email address.</param>
        /// <param name="subject">The subject line of the email.</param>
        /// <param name="message">The HTML content of the email body.</param>
        /// <returns>A task that represents the asynchronous operation of sending the email.</returns>
        /// <exception cref="Exception">Thrown if the "SendGridKey" is not found in the application configuration.</exception>
        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // This line reads the secret API key from configuration (e.g., secrets.json) stored by project->rmb->Manage user Secrets.
            // This is the *only* configuration needed. The official SendGridClient library
            // handles all Host and Port settings internally, which is why the old "SmtpSettings"
            // block is no longer needed in appsettings.json.

            var apiKey = _configuration["SendGridKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                // This exception helps you debug if the key isn't found.
                throw new Exception("The 'SendGridKey' was not found in the configuration. Ensure it is set in user secrets.");
            }

            var client = new SendGridClient(apiKey);

            // IMPORTANT: Replace this with an email address you have verified in your SendGrid account.
            //svar from = new EmailAddress("nuno.goncalo.gomes@formandos.cinel.pt", "CondoManagerPrime Support");
            var from = new EmailAddress("nmiguelgomes2025@gmail.com", "CondoManagerPrime Support");
            var to = new EmailAddress(toEmail);

            // The 'message' parameter already contains the HTML for the email body.
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", message);

            // Send the email via SendGrid's API.
            var response = await client.SendEmailAsync(msg);

            // Optional: You can add logging here to check the response status code
            // to see if the email was sent successfully.
            if (!response.IsSuccessStatusCode)
            {
                // Log the error if sending failed
                Console.WriteLine($"Failed to send email: {response.StatusCode}");
            }
        }
    }
}
