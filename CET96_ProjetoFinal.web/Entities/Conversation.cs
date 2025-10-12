using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Entities
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

        // Navigation property holding all messages belonging to this conversation.
        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}