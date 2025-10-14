using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CET96_ProjetoFinal.web.Data.Entities
{
    /// <summary>
    /// Represents a single conversation thread, which groups multiple messages together.
    /// </summary>
    public class Conversation : IEntity
    {
        // The unique identifier for the conversation.
        public int Id { get; set; }

        // The subject or title of the conversation (e.g., "Water Leak in Unit 5B").
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; }

        // The timestamp when the conversation was first created.
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// The current workflow status of the conversation (e.g., Pending, Resolved).
        /// </summary>
        public MessageStatus Status { get; set; } = MessageStatus.Pending;

        /// <summary>
        /// The ID of the user who created the conversation thread.
        /// </summary>
        /// <remarks>
        /// This user is the originator of the conversation, regardless of their role
        /// (e.g., an owner filing a complaint or a manager sending a notice).
        /// </remarks>
        public string InitiatorId { get; set; }

        /// <summary>
        /// The ID of the user to whom this conversation has been assigned for handling or resolution.
        /// </summary>
        /// <remarks>
        /// This is nullable, as a conversation may not be assigned to a specific person initially.
        /// For example, a manager might assign a maintenance request to a specific staff member.
        /// </remarks>
        public string? AssignedToId { get; set; }

        /// <summary>
        /// The ID of the unit this conversation is related to.
        /// </summary>
        public int UnitId { get; set; }

        // Navigation property holding all messages belonging to this conversation.
        public ICollection<Message> Messages { get; set; } = new List<Message>();

        // --- Not Mapped Properties for Cross-DB Joins ---

        /// <summary>
        /// Navigation property to the initiator (user).
        /// Not mapped to the database to avoid cross-context foreign key issues.
        /// This should be populated manually in the controller.
        /// </summary>
        [NotMapped]
        public ApplicationUser? Initiator { get; set; }

        /// <summary>
        /// Navigation property to the assigned user.
        /// Not mapped to the database. Populate manually in the controller.
        /// </summary>
        [NotMapped]
        public ApplicationUser? AssignedTo { get; set; }

        /// <summary>
        /// Navigation property to the related unit.
        /// Not mapped to the database. Populate manually in the controller.
        /// </summary>
        [NotMapped]
        public Unit? Unit { get; set; }
    }
}