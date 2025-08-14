using System.Diagnostics;

namespace CET96_ProjetoFinal.web.Services
{
    public class DebugEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            // Using Debug.WriteLine to output the information.
            // This will appear in the "Output" window in Visual Studio
            // when you are running the application in Debug mode.
            Debug.WriteLine("--- NEW EMAIL ---");
            Debug.WriteLine($"To: {email}");
            Debug.WriteLine($"Subject: {subject}");
            Debug.WriteLine("Body:");
            Debug.WriteLine(message);
            Debug.WriteLine("--- END OF EMAIL ---");

            // Return a completed task since this operation is synchronous.
            return Task.CompletedTask;
        }
    }
}
