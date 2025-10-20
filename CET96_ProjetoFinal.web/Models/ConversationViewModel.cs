namespace CET96_ProjetoFinal.web.Models
{
    /// <summary>
    /// Represents a conversation entry in the messaging system.
    /// Used to display conversations in the message center,
    /// including metadata like subject, status, participants, roles,
    /// assignment info, and UI helpers (CSS classes, unit identifiers).
    /// </summary>
    public class ConversationViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Starter (initiator) info
        public string? StarterName { get; set; }           // e.g. "João Pereira"
        public string? StarterRole { get; set; }           // e.g. "Unit Owner", "Manager"

        // Other participant info (legacy / optional)
        public string? OtherParticipantName { get; set; }
        public string? OtherParticipantRole { get; set; }

        // Visual helpers
        public string? OtherRoleBadgeCss { get; set; }     // e.g. "badge bg-primary"
        public string? StatusCss { get; set; }            // e.g. "dot-pending", "dot-resolved"
        public string? UnitNumber { get; set; }           // e.g. "A-302"

        // Assignment info (optional)
        public string? AssignedToName { get; set; }       // e.g. "Maria Silva"
        public string? AssignedToRole { get; set; }       // e.g. "Condominium Staff"

        // Initiator role + permission flags
        public string? InitiatorRole { get; set; }
        public bool CanAssign { get; set; }

        // Unread badge & status action flags + workflow permissions
        public int UnreadCount { get; set; }                  // unread messages for the current user
        public bool CanMarkInProgress { get; set; }           // show “In Progress” button
        public bool CanResolve { get; set; }                  // show “Resolve” button
        public bool CanClose { get; set; }                    // show “Close” button
    }
}
