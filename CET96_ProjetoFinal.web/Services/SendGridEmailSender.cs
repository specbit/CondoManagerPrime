using SendGrid;
using SendGrid.Helpers.Mail;

namespace CET96_ProjetoFinal.web.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public SendGridEmailSender(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string message)
        {
            // This line reads the secret API key you stored earlier.
            var apiKey = _configuration["SendGridKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                // This exception helps you debug if the key isn't found.
                throw new Exception("The 'SendGridKey' was not found in the configuration. Ensure it is set in user secrets.");
            }

            var client = new SendGridClient(apiKey);

            // IMPORTANT: Replace this with an email address you have verified in your SendGrid account.
            var from = new EmailAddress("nuno.goncalo.gomes@formandos.cinel.pt", "CondoManagerPrime Support");

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
