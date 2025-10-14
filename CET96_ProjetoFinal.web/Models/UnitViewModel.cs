namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents a single unit for display in a list, including the assigned owner's name.
    /// </summary>
    public class UnitViewModel
    {
        public int Id { get; set; }
        public string UnitNumber { get; set; }
        public bool IsActive { get; set; }

        // This property will hold the owner's full name or "(Not Assigned)"
        public string OwnerName { get; set; }
    }
}