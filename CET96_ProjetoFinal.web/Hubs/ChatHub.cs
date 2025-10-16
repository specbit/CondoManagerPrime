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

        /// <summary>
        /// Called by the client-side JavaScript when a user clicks on a conversation.
        /// This method adds the user's current connection to a private group for that conversation.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to join.</param>
        public async Task JoinConversationGroup(int conversationId)
        {
            // The group name is simply the conversation's ID converted to a string.
            string groupName = conversationId.ToString();
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        /// <summary>
        /// Called by the client-side JavaScript when a user sends a message.
        /// This method saves the message and then sends it ONLY to the members of the specific conversation group.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation the message belongs to.</param>
        /// <param name="messageContent">The text of the message.</param>
        public async Task SendMessage(int conversationId, string messageContent)
        {
            var sender = await _userManager.GetUserAsync(Context.User);
            if (sender == null) return; // Safety check

            // 1. Create and save the message to the database.
            var message = new Message
            {
                ConversationId = conversationId,
                Content = messageContent,
                SenderId = sender.Id,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // 2. Prepare the message data to be sent to the clients.
            var messageViewModel = new
            {
                content = message.Content,
                sentAt = message.SentAt.ToString("o"), // ISO 8601 format for JavaScript
                senderName = $"{sender.FirstName} {sender.LastName}"
            };

            string groupName = conversationId.ToString();

            // 3. Send the message ONLY to the group for this conversation.
            // This is the crucial fix. It replaces "Clients.All".
            await Clients.Group(groupName).SendAsync("ReceiveMessage", messageViewModel);
        }

        // TODO: delete after testing
        //// A client will call this method to send a message.
        //// The server will then broadcast this message to all other clients.
        //public async Task SendMessage(int conversationId, string messageContent)
        //{
        //    // 1. Get the ID of the user who is sending the message.
        //    var senderId = Context.UserIdentifier; // This is the user's ID from their login.
        //    var sender = await _userManager.FindByIdAsync(senderId);

        //    // 2. Create the Message entity to save to the database.
        //    var message = new Message
        //    {
        //        ConversationId = conversationId,
        //        Content = messageContent,
        //        SenderId = senderId,
        //        SentAt = DateTime.UtcNow
        //    };

        //    // 3. Save the new message to the database.
        //    _context.Messages.Add(message);
        //    await _context.SaveChangesAsync();

        //    // 4. Find the other participant in the conversation to send them the message.
        //    // (This is a simplified logic for now; we'll enhance it later).
        //    // For now, we'll just broadcast it back to the sender to confirm it was sent.

        //    // The object we send to the client-side JavaScript.
        //    var messageViewModel = new
        //    {
        //        content = message.Content,
        //        sentAt = message.SentAt.ToString("o"), // ISO 8601 format
        //        senderName = $"{sender.FirstName} {sender.LastName}"
        //    };

        //    // 5. Broadcast the new message only to clients in this conversation.
        //    // (For now, we'll use a placeholder group name).
        //    await Clients.All.SendAsync("ReceiveMessage", messageViewModel);
        //}

        /// <summary>
        /// Called by the client-side JavaScript when a user switches to a different conversation.
        /// This method removes the user's current connection from a group.
        /// </summary>
        /// <param name="conversationId">The ID of the conversation to leave.</param>
        public async Task LeaveConversationGroup(int conversationId)
        {
            string groupName = conversationId.ToString();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
    }
}