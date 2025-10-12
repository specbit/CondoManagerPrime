using CET96_ProjetoFinal.web.Enums;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Entities
{
    /// <summary>
    /// Represents a single message within a conversation.
    /// </summary>
    public class Message : IEntity
    {
        public int Id { get; set; }

        [Required]
        public string Content { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // --- Relationships (as simple IDs, no navigation properties to ApplicationUser) ---

        [Required]
        public string SenderId { get; set; } // Just the ID

        public string? ReceiverId { get; set; } // Just the ID

        public int ConversationId { get; set; }
        public Conversation Conversation { get; set; } // This is OK, it's in the same DB

        // --- Tracking & Workflow ---
        public bool IsRead { get; set; } = false;
        public MessageStatus Status { get; set; } = MessageStatus.Pending;
    }
}