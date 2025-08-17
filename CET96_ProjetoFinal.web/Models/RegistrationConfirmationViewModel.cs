namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents the data required to confirm a user registration.
    /// </summary>
    /// <remarks>This view model is typically used to provide the confirmation link to the user after a
    /// successful registration process.</remarks>
    public class RegistrationConfirmationViewModel
    {
        public string ConfirmationLink { get; set; }
    }
}
