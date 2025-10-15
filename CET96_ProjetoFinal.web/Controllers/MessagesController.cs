using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Data.Entities;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Enums;
using CET96_ProjetoFinal.web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly CondominiumDataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(CondominiumDataContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // The main action to display the messaging page
        public async Task<IActionResult> Index()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1. Get all conversations involving the current user
            var conversations = await _context.Conversations
                .Where(c => c.InitiatorId == currentUserId || c.AssignedToId == currentUserId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // 2. Get a unique list of all other user IDs from these conversations
            var userIds = conversations
                .Select(c => c.InitiatorId == currentUserId ? c.AssignedToId : c.InitiatorId)
                .Where(id => id != null)
                .Distinct()
                .ToList();

            // 3. Fetch all those users from the database in a single query
            var users = await _userManager.Users.Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id);

            // 4. Create a list of ViewModels to pass to the view
            var model = conversations.Select(c => {
                var otherUserId = c.InitiatorId == currentUserId ? c.AssignedToId : c.InitiatorId;
                var otherUser = otherUserId != null && users.ContainsKey(otherUserId) ? users[otherUserId] : null;

                return new ConversationViewModel
                {
                    Id = c.Id,
                    Subject = c.Subject,
                    Status = c.Status.ToString(),
                    OtherParticipantName = otherUser != null ? $"{otherUser.FirstName} {otherUser.LastName}" : "System"
                };
            }).ToList();

            return View(model);
        }

        // GET: /Messages/Create
        /// <summary>
        /// Displays the form for a Unit Owner to create a new conversation.
        /// </summary>
        [Authorize(Roles = "Unit Owner")]
        public async Task<IActionResult> Create()
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Find the unit assigned to the currently logged-in owner.
            var assignedUnit = await _context.Units.FirstOrDefaultAsync(u => u.OwnerId == currentUserId);
            if (assignedUnit == null)
            {
                TempData["StatusMessage"] = "Error: You are not assigned to a unit, so you cannot create a conversation.";
                return RedirectToAction("Index", "Home");
            }

            // Find the manager of the condominium this unit belongs to.
            var condominium = await _context.Condominiums.FindAsync(assignedUnit.CondominiumId);
            if (condominium == null || string.IsNullOrEmpty(condominium.CondominiumManagerId))
            {
                TempData["StatusMessage"] = "Error: This unit's condominium does not have a manager assigned.";
                return RedirectToAction("Index", "Home");
            }

            var model = new CreateConversationViewModel
            {
                UnitId = assignedUnit.Id,
                CondominiumManagerId = condominium.CondominiumManagerId
            };

            return View(model);
        }

        // POST: /Messages/Create
        /// <summary>
        /// Handles the submission of the new conversation form.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Unit Owner")]
        public async Task<IActionResult> Create(CreateConversationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var initiatorId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // 1. Create the main Conversation object (the "ticket").
                var conversation = new Conversation
                {
                    Subject = model.Subject,
                    InitiatorId = initiatorId,
                    AssignedToId = model.CondominiumManagerId, // Initially assign to the manager
                    UnitId = model.UnitId,
                    Status = MessageStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync(); // Save to get the new ConversationId

                // 2. Create the first Message in that conversation.
                var message = new Message
                {
                    Content = model.Message,
                    SenderId = initiatorId,
                    ReceiverId = model.CondominiumManagerId, // The first message goes to the manager
                    ConversationId = conversation.Id, // Link to the conversation we just created
                    SentAt = DateTime.UtcNow
                };
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // TODO: Use SignalR to notify the manager in real-time.

                TempData["StatusMessage"] = "Your new conversation has been created successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // This action is called by JavaScript to get a conversation's message history
        [HttpGet]
        public async Task<IActionResult> GetMessagesForConversation(int conversationId)
        {
            // 1. Get all messages for the conversation from the CondominiumDB.
            var messages = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt)
                .ToListAsync();

            // 2. From that list, get all the unique sender IDs.
            var senderIds = messages.Select(m => m.SenderId).Distinct().ToList();

            // 3. Get all the user details for those senders from the ApplicationUserDB in a single query.
            var senders = await _userManager.Users
                .Where(u => senderIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            // 4. In C# code (not in the database), combine the data to create the final result.
            var result = messages.Select(m => new
            {
                content = m.Content,
                sentAt = m.SentAt.ToString("o"), // Use a standard format like ISO 8601 for JavaScript
                senderName = senders.ContainsKey(m.SenderId)
                             ? $"{senders[m.SenderId].FirstName} {senders[m.SenderId].LastName}"
                             : "Unknown User"
            });

            return Json(result);
        }
    }
}