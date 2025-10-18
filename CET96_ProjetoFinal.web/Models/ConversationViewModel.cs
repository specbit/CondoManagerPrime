namespace CET96_ProjetoFinal.web.Models
{
    public class ConversationViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public string OtherParticipantName { get; set; }
        public DateTime CreatedAt { get; set; }

        // PROPERTIES FOR BADGE, ROLE, STATUS DOT, AND UNIT
        public string OtherParticipantRole { get; set; }           // e.g. "Manager", "Owner", "Staff"
        public string OtherRoleBadgeCss { get; set; }              // e.g. "badge bg-primary"
        public string StatusCss { get; set; }                      // e.g. "dot-pending", "dot-resolved"
        public string UnitNumber { get; set; }                     // e.g. "A-302"

    }
}