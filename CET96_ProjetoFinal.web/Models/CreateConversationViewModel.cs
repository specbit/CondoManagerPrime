using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class CreateConversationViewModel
    {
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required]
        public string Message { get; set; }

        // Hidden fields needed for creating the conversation
        public int UnitId { get; set; }
        public string CondominiumManagerId { get; set; }
    }
}