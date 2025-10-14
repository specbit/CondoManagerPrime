using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;

namespace CET96_ProjetoFinal.web.Hubs
{
    public class ChatHub : Hub
    {
        private readonly CondominiumDataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatHub(CondominiumDataContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // A client will call this method to send a message.
        // The server will then broadcast this message to all other clients.
        public async Task SendMessage(int conversationId, string messageContent)
        {
            // 1. Get the ID of the user who is sending the message.
            var senderId = Context.UserIdentifier; // This is the user's ID from their login.
            var sender = await _userManager.FindByIdAsync(senderId);

            // 2. Create the Message entity to save to the database.
            var message = new Message
            {
                ConversationId = conversationId,
                Content = messageContent,
                SenderId = senderId,
                SentAt = DateTime.UtcNow
            };

            // 3. Save the new message to the database.
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // 4. Find the other participant in the conversation to send them the message.
            // (This is a simplified logic for now; we'll enhance it later).
            // For now, we'll just broadcast it back to the sender to confirm it was sent.

            // The object we send to the client-side JavaScript.
            var messageViewModel = new
            {
                content = message.Content,
                sentAt = message.SentAt.ToString("o"), // ISO 8601 format
                senderName = $"{sender.FirstName} {sender.LastName}"
            };

            // 5. Broadcast the new message only to clients in this conversation.
            // (For now, we'll use a placeholder group name).
            await Clients.All.SendAsync("ReceiveMessage", messageViewModel);
        }
    }
}