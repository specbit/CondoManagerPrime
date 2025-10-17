namespace CET96_ProjetoFinal.web.Models
{
    public class ConversationViewModel
    {
        public int Id { get; set; }
        public string Subject { get; set; }
        public string Status { get; set; }
        public string OtherParticipantName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}