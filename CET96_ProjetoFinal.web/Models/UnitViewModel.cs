namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents a single unit for display in a list, including details about the assigned owner.
    /// </summary>
    public class UnitViewModel
    {
        public int Id { get; set; }

        public string UnitNumber { get; set; } //  The number or name of the unit (e.g., "1A", "Floor 3, Apt B").

        public bool IsActive { get; set; }

        public string OwnerName { get; set; } // This property will hold the owner's full name or "(Not Assigned)"

        // This property holds the ID of the assigned owner.
        // It's needed to power the "intelligent button" logic in the view.
        // Checking if OwnerId is null or empty is a more reliable way to decide
        // whether to show the "Assign Owner" or "Dismiss Owner" button than checking
        // the OwnerName string.
        public string? OwnerId { get; set; }
    }
}