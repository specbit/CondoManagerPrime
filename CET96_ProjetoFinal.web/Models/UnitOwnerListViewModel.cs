namespace CET96_ProjetoFinal.web.Models
{
    // We created this new ViewModel because the "Manage Owners" list page has different
    // data needs than the "Create/Edit Owner" form. This list needs to show the owner's
    // assignment status, but it doesn't need sensitive or irrelevant data like passwords
    // or dropdown lists. Using a separate, specialized ViewModel like this is cleaner,
    // more efficient, and more secure.
    public class UnitOwnerListViewModel
    {
        // The unique ID of the user, needed for the Edit/Details links.
        public string Id { get; set; }

        // The user's full name (e.g., "Zé Santos").
        public string FullName { get; set; }

        // The user's email address.
        public string Email { get; set; }

        // The user's phone number.
        public string PhoneNumber { get; set; }

        // A flag to check if the account has been deactivated.
        public bool IsActive { get; set; }

        // A flag to check if the user has confirmed their email.
        public bool IsConfirmed { get; set; }

        // The new flag to check if the owner is currently assigned to a unit.
        public bool IsAssigned { get; set; }
    }
}