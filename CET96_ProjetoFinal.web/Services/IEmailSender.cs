namespace CET96_ProjetoFinal.web.Services
{
    public interface IEmailSender
    {
        /// <summary>
        /// Sends an email asynchronously with the specified recipient, subject, and message content.
        /// </summary>
        /// <param name="email">The email address of the recipient. This cannot be null or empty.</param>
        /// <param name="subject">The subject of the email. This cannot be null or empty.</param>
        /// <param name="message">The body content of the email. This cannot be null or empty.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        Task SendEmailAsync(string email, string subject, string message);
    }
}
