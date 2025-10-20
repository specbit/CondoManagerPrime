namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents a conversation entry in the messaging system.
    /// This view model is used to display conversations in the message center,
    /// including metadata such as subject, status, participants, roles,
    /// assignment information, and visual UI helpers like CSS classes and unit identifiers.
    /// </summary>
    public class ConversationViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }

        // NEW: Starter (initiator) info — used instead of the confusing "From:" field
        public string StarterName { get; set; }                  // e.g. "João Pereira" (conversation initiator)
        public string StarterRole { get; set; }                  // e.g. "Unit Owner", "Condominium Manager"

        // Other participant info (kept for compatibility, may be hidden in UI)
        public string OtherParticipantName { get; set; }

        // PROPERTIES FOR BADGE, ROLE, STATUS DOT, AND UNIT
        public string OtherParticipantRole { get; set; }           // e.g. "Manager", "Owner", "Staff"
        public string OtherRoleBadgeCss { get; set; }              // e.g. "badge bg-primary"
        public string StatusCss { get; set; }                      // e.g. "dot-pending", "dot-resolved"
        public string UnitNumber { get; set; }                     // e.g. "A-302"

        // Assignment info (for "To:" display) — now optional; UI can use Starter + Assignee instead
        public string AssignedToName { get; set; }                // e.g. "Maria Silva" (staff handling it)
        public string AssignedToRole { get; set; }                // e.g. "Condominium Staff"

        // Initiator role + can-assign flag
        public string InitiatorRole { get; set; }
        public bool CanAssign { get; set; }
    }
}
