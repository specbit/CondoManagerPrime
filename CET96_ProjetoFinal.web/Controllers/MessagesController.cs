using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Data.Entities;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Enums;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CET96_ProjetoFinal.web.Controllers
{
    [Authorize]
    public class MessagesController : Controller
    {
        private readonly CondominiumDataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICompanyRepository _companyRepository;

        public MessagesController(
            CondominiumDataContext context, 
            UserManager<ApplicationUser> userManager,
            ICompanyRepository companyRepository)
        {
            _context = context;
            _userManager = userManager;
            _companyRepository = companyRepository;
        }

        // The main action to display the messaging page
        public async Task<IActionResult> Index(int? condominiumId)
        {
            ViewBag.CondominiumId = condominiumId;

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
                    OtherParticipantName = otherUser != null ? $"{otherUser.FirstName} {otherUser.LastName}" : "System",
                    CreatedAt = c.CreatedAt
                };
            }).ToList();

            return View(model);
        }

        // In MessagesController.cs

        // GET: /Messages/Create
        /// <summary>
        /// Displays the form for a user to create a new conversation.
        /// It intelligently populates the recipient list based on the user's role.
        /// </summary>
        [Authorize(Roles = "Unit Owner, Condominium Manager, Company Administrator, Condominium Staff")]
        public async Task<IActionResult> Create(int? condominiumId)
        {
            ViewBag.CondominiumId = condominiumId; // For return url

            var model = new CreateConversationViewModel();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var recipients = new List<SelectListItem>();

            // --- NEW: Smart Recipient Logic ---
            if (User.IsInRole("Unit Owner"))
            {
                // An Owner can message their Manager and the Company Admin.
                var assignedUnit = await _context.Units.FirstOrDefaultAsync(u => u.OwnerId == currentUser.Id);
                if (assignedUnit != null)
                {
                    var condominium = await _context.Condominiums.FindAsync(assignedUnit.CondominiumId);
                    if (condominium != null)
                    {
                        // Add the Condo Manager
                        if (!string.IsNullOrEmpty(condominium.CondominiumManagerId))
                        {
                            var manager = await _userManager.FindByIdAsync(condominium.CondominiumManagerId);
                            if (manager != null)
                                recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
                        }
                        // Add the Company Admin
                        var company = await _companyRepository.GetByIdAsync(condominium.CompanyId);
                        if (company != null && !string.IsNullOrEmpty(company.ApplicationUserId))
                        {
                            var admin = await _userManager.FindByIdAsync(company.ApplicationUserId);
                            if (admin != null)
                                recipients.Add(new SelectListItem($"{admin.FirstName} {admin.LastName} (Admin)", admin.Id));
                        }
                    }
                }
            }
            else if (User.IsInRole("Company Administrator"))
            {
                // A Company Admin can message all the managers in their company.
                var allManagers = await _userManager.GetUsersInRoleAsync("Condominium Manager");
                var managersInCompany = allManagers.Where(m => m.CompanyId == currentUser.CompanyId);

                foreach (var manager in managersInCompany)
                {
                    recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
                }
            }
            // TODO: add logic for other roles (Manager, Staff, etc.) here in the future.

            model.Recipients = recipients;
            return View(model);
        }

        // POST: /Messages/Create
        /// <summary>
        /// Handles the submission of the new conversation form.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Unit Owner, Condominium Manager, Company Administrator, Condominium Staff")]
        public async Task<IActionResult> Create(CreateConversationViewModel model)
        {
            // Get the user who is initiating the conversation.
            var initiator = await _userManager.GetUserAsync(User);
            if (initiator == null) return Unauthorized();

            // --- START: THE FIX ---
            // The recipient list must be re-populated every time this action is called,
            // both for the initial GET and for POSTs that fail validation.
            // This prevents the dropdown from being empty if the page needs to be re-displayed with an error.
            var recipients = new List<SelectListItem>();
            if (User.IsInRole("Unit Owner"))
            {
                var assignedUnit = await _context.Units.FirstOrDefaultAsync(u => u.OwnerId == initiator.Id);
                if (assignedUnit != null)
                {
                    var condominium = await _context.Condominiums.FindAsync(assignedUnit.CondominiumId);
                    if (condominium != null)
                    {
                        // Add the Condo Manager
                        if (!string.IsNullOrEmpty(condominium.CondominiumManagerId))
                        {
                            var manager = await _userManager.FindByIdAsync(condominium.CondominiumManagerId);
                            if (manager != null)
                                recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
                        }
                        // Add the Company Admin
                        var company = await _companyRepository.GetByIdAsync(condominium.CompanyId);
                        if (company != null && !string.IsNullOrEmpty(company.ApplicationUserId))
                        {
                            var admin = await _userManager.FindByIdAsync(company.ApplicationUserId);
                            if (admin != null)
                                recipients.Add(new SelectListItem($"{admin.FirstName} {admin.LastName} (Admin)", admin.Id));
                        }
                    }
                }
            }
            // We will add logic for other roles (e.g., Manager, Staff) here in the future.
            model.Recipients = recipients;
            // --- END: THE FIX ---

            if (ModelState.IsValid)
            {
                // Find the unit associated with the initiator (for now, we assume it's an owner).
                var userId = initiator.Id;               // string
                var unit = await _context.Units
                    .FirstOrDefaultAsync(u => u.OwnerId == userId);

                if (unit == null)
                {
                    ModelState.AddModelError("", "Could not find an associated unit for this user.");
                    // Because we repopulated the list above, we can now safely return the view.
                    return View(model);
                }

                // 1. Create the Conversation object.
                var conversation = new Conversation
                {
                    Subject = model.Subject,
                    InitiatorId = initiator.Id,
                    AssignedToId = model.RecipientId, // Initially assigned to the chosen recipient
                    UnitId = unit.Id,
                    Status = MessageStatus.Pending
                };
                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync(); // Save to get the new ConversationId

                // 2. Create the first Message in that conversation.
                var message = new Message
                {
                    Content = model.Message,
                    SenderId = initiator.Id,
                    ReceiverId = model.RecipientId, // The first message goes to the chosen recipient
                    ConversationId = conversation.Id,
                };
                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // TODO: Use SignalR to notify the specific recipient in real-time.

                TempData["StatusMessage"] = "Your new conversation has been created successfully.";
                return RedirectToAction(nameof(Index));
            }

            // If the initial model state was invalid (e.g., no recipient was selected),
            // the view is returned with the correctly re-populated dropdown list.
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

        /// <summary>
        /// This action renders the dashboard view for messaging for the roles "Company Administrator" and "Condominium Manager"
        /// </summary>
        /// <returns></returns>
        public IActionResult Dashboard(int? condominiumId)
        {
            ViewBag.CondominiumId = condominiumId;
            return View();
        }
    }
}