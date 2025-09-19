namespace CET96_ProjetoFinal.web.Models
{
    public class ApplicationUserViewModel
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string UserName { get; set; }

        public bool IsEmailConfirmed { get; set; }

        // List of strings to hold multiple roles
        public IList<string> Roles { get; set; }

        //Track the user's status for the badge and button
        public bool IsDeactivated { get; set; }

        public string? AssignedCondominiumName { get; set; }
    }
}
