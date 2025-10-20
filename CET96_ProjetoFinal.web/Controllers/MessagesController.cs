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
        private readonly IApplicationUserRepository _userRepository;
        private readonly ICondominiumRepository _condominiumRepository;

        public MessagesController(
            CondominiumDataContext context,
            UserManager<ApplicationUser> userManager,
            ICompanyRepository companyRepository,
            IApplicationUserRepository userRepository,
            ICondominiumRepository condominiumRepository)
        {
            _context = context;
            _userManager = userManager;
            _companyRepository = companyRepository;
            _userRepository = userRepository;
            _condominiumRepository = condominiumRepository;
        }

        // The main action to display the messaging page

        /// <summary>
        /// Displays the messaging inbox for the current user.
        /// Shows all conversations where the user is either the initiator or the assignee,
        /// ordered by most recent first, and enriches them with metadata for UI display.
        /// </summary>
        /// <param name="condominiumId">
        /// Optional condominium ID used for navigation context (e.g. return links).
        /// </param>
        /// <returns>
        /// A view containing a list of <see cref="ConversationViewModel"/> items, each with
        /// status dot, participant details, role badges, and unit number (if applicable).
        /// </returns>
        public async Task<IActionResult> Index(int? condominiumId)
        {
            ViewBag.CondominiumId = condominiumId;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1) Conversations (no Include)
            var conversations = await _context.Conversations
                .Where(c => c.InitiatorId == currentUserId || c.AssignedToId == currentUserId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // 1a) Units for those conversations
            var unitIds = conversations
                .Select(c => c.UnitId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var unitNumbersById = await _context.Units
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

            // 2) Other user ids
            var userIds = conversations
                .Select(c => c.InitiatorId == currentUserId ? c.AssignedToId : c.InitiatorId)
                .Where(id => id != null)
                .Distinct()
                .ToList();

            // 3) Users
            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            // 3a) Roles -> badge css
            var rolesByUser = new Dictionary<string, (string Role, string BadgeCss)>();
            foreach (var kv in users)
            {
                var usr = kv.Value;
                var roles = await _userManager.GetRolesAsync(usr);
                var role = roles.FirstOrDefault() ?? "User";
                var badgeCss = role switch
                {
                    "Company Administrator" => "bg-dark",
                    "Condominium Manager" => "bg-primary",
                    "Condominium Staff" => "bg-info",
                    "Unit Owner" => "bg-success",
                    _ => "bg-secondary"
                };
                rolesByUser[usr.Id] = (role, badgeCss);
            }

            // 4) Project VM
            var model = conversations.Select(c =>
            {
                var otherUserId = c.InitiatorId == currentUserId ? c.AssignedToId : c.InitiatorId;
                users.TryGetValue(otherUserId ?? "", out var otherUser);

                var (role, badge) = (otherUserId != null && rolesByUser.TryGetValue(otherUserId, out var tuple))
                    ? tuple : ("User", "bg-secondary");

                // map your enum -> css class for the status dot
                var statusCss = c.Status switch
                {
                    MessageStatus.Pending => "status-pending",
                    MessageStatus.Assigned => "status-assigned",
                    MessageStatus.InProgress => "status-inprogress",
                    MessageStatus.Resolved => "status-resolved",
                    MessageStatus.Closed => "status-closed",
                    _ => "status-closed"
                };

                // Only assign a unit number if the conversation is linked to a unit
                var unitNumber = unitNumbersById.TryGetValue(c.UnitId, out var num) ? num : null;

                return new ConversationViewModel
                {
                    Id = c.Id,
                    Subject = c.Subject,
                    Status = c.Status.ToString(),
                    OtherParticipantName = otherUser != null ? $"{otherUser.FirstName} {otherUser.LastName}" : "System",
                    OtherParticipantRole = role,
                    OtherRoleBadgeCss = badge,
                    StatusCss = statusCss,
                    UnitNumber = role == "Unit Owner" ? unitNumber : null,
                    CreatedAt = c.CreatedAt
                };
            }).ToList();

            return View(model);
        }


        // GET: /Messages/Create
        /// <summary>
        /// Displays the "Create Conversation" form and dynamically builds the recipient list
        /// based on the current user's role and the specified condominium.
        /// 
        /// <para><strong>Business rules:</strong></para>
        /// <list type="bullet">
        ///   <item>
        ///     <description><strong>Company Administrator:</strong> Can only message the manager (exactly 1), all staff, and all owners of the specified condominium.</description>
        ///   </item>
        ///   <item>
        ///     <description><strong>Condominium Manager:</strong> Can message the company administrator, all staff, and all owners of the condominium they manage.</description>
        ///   </item>
        ///   <item>
        ///     <description><strong>Condominium Staff:</strong> Can message the manager and all owners of their assigned condominium.</description>
        ///   </item>
        ///   <item>
        ///     <description><strong>Unit Owner:</strong> Can message their condominium’s manager and the company administrator.</description>
        ///   </item>
        /// </list>
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium where the message is being created. Required for Company Administrators.</param>
        /// <param name="recipientId">Optional pre-selected recipient ID.</param>
        /// <returns>The Create Conversation view with a dynamically populated recipient list.</returns>
        [Authorize(Roles = "Unit Owner, Condominium Manager, Company Administrator, Condominium Staff")]
        [HttpGet]
        public async Task<IActionResult> Create(int? condominiumId, string? recipientId = null)
        {
            ViewBag.CondominiumId = condominiumId;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            var model = new CreateConversationViewModel { RecipientId = recipientId };
            model.CondominiumId = condominiumId;

            var recipients = new List<SelectListItem>();

            // --- Smart Recipient Logic ---
            if (User.IsInRole("Company Administrator"))
            {
                if (!condominiumId.HasValue) return BadRequest("condominiumId is required for company administrators.");

                var condo = await _context.Condominiums.FindAsync(condominiumId.Value);
                if (condo == null) return NotFound("Condominium not found.");

                // 1) Manager (exactly one)
                if (!string.IsNullOrEmpty(condo.CondominiumManagerId))
                {
                    var manager = await _userManager.FindByIdAsync(condo.CondominiumManagerId);
                    if (manager != null && manager.Id != currentUser.Id)
                    {
                        recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));

                    }
                }

                // Get owner IDs first (so we can exclude them from staff)
                var ownerIds = await _context.Units
                    .Where(u => u.CondominiumId == condo.Id && u.OwnerId != null)
                    .Select(u => u.OwnerId!)
                    .Distinct()
                    .ToListAsync();
                var ownerIdSet = ownerIds.ToHashSet();

                // 2) Staff for this condo, EXCLUDING owners (owners will be labeled as Owners)
                var staffInCondo = await _userRepository.GetStaffByCondominiumIdAsync(condo.Id);
                foreach (var s in staffInCondo.Where(s => s.Id != currentUser.Id && !ownerIdSet.Contains(s.Id)))
                {
                    recipients.Add(new SelectListItem($"{s.FirstName} {s.LastName} (Staff)", s.Id));
                }

                // 3) Owners for this condo (will catch anyone who’s both staff+owner and label them as Owner)
                var owners = await _userManager.Users
                    .Where(u => ownerIdSet.Contains(u.Id))
                    .ToListAsync();

                foreach (var o in owners.Where(o => o.Id != currentUser.Id))
                {
                    recipients.Add(new SelectListItem($"{o.FirstName} {o.LastName} (Owner)", o.Id));
                }
            }
            else if (User.IsInRole("Condominium Manager"))
            {
                var managedCondo = await _condominiumRepository.GetCondominiumByManagerIdAsync(currentUser.Id);
                if (managedCondo != null)
                {
                    // Company Admin
                    var company = await _companyRepository.GetByIdAsync(managedCondo.CompanyId);
                    if (company?.ApplicationUserId is string adminId)
                    {
                        var admin = await _userManager.FindByIdAsync(adminId);
                        if (admin != null && admin.Id != currentUser.Id)
                            recipients.Add(new SelectListItem($"{admin.FirstName} {admin.LastName} (Admin)", admin.Id));
                    }

                    // Staff
                    var staff = await _userRepository.GetStaffByCondominiumIdAsync(managedCondo.Id);
                    recipients.AddRange(staff.Select(s =>
                        new SelectListItem($"{s.FirstName} {s.LastName} (Staff)", s.Id)));

                    // Owners
                    var ownerIds = await _context.Units
                        .Where(u => u.CondominiumId == managedCondo.Id && u.OwnerId != null)
                        .Select(u => u.OwnerId!)
                        .Distinct()
                        .ToListAsync();

                    var owners = await _userManager.Users
                        .Where(u => ownerIds.Contains(u.Id))
                        .ToListAsync();

                    recipients.AddRange(owners.Select(o =>
                        new SelectListItem($"{o.FirstName} {o.LastName} (Owner)", o.Id)));
                }
            }
            else if (User.IsInRole("Condominium Staff"))
            {
                if (currentUser.CondominiumId.HasValue)
                {
                    var condo = await _condominiumRepository.GetByIdAsync(currentUser.CondominiumId.Value);
                    if (condo != null)
                    {
                        // Manager
                        if (!string.IsNullOrEmpty(condo.CondominiumManagerId))
                        {
                            var manager = await _userManager.FindByIdAsync(condo.CondominiumManagerId);
                            if (manager != null)
                                recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
                        }

                        // Owners
                        var ownerIds = await _context.Units
                            .Where(u => u.CondominiumId == condo.Id && u.OwnerId != null)
                            .Select(u => u.OwnerId!)
                            .Distinct()
                            .ToListAsync();

                        var owners = await _userManager.Users
                            .Where(u => ownerIds.Contains(u.Id))
                            .ToListAsync();

                        recipients.AddRange(owners.Select(o =>
                            new SelectListItem($"{o.FirstName} {o.LastName} (Owner)", o.Id)));
                    }
                }
            }
            else if (User.IsInRole("Unit Owner"))
            {
                var assignedUnit = await _context.Units.FirstOrDefaultAsync(u => u.OwnerId == currentUser.Id);
                if (assignedUnit != null)
                {
                    var condo = await _context.Condominiums.FindAsync(assignedUnit.CondominiumId);
                    if (condo != null)
                    {
                        // Manager
                        if (!string.IsNullOrEmpty(condo.CondominiumManagerId))
                        {
                            var manager = await _userManager.FindByIdAsync(condo.CondominiumManagerId);
                            if (manager != null)
                                recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
                        }

                        // Company Admin
                        var company = await _companyRepository.GetByIdAsync(condo.CompanyId);
                        if (company?.ApplicationUserId is string adminId)
                        {
                            var admin = await _userManager.FindByIdAsync(adminId);
                            if (admin != null)
                                recipients.Add(new SelectListItem($"{admin.FirstName} {admin.LastName} (Admin)", admin.Id));
                        }
                    }
                }
            }

            // Deduplicate and sort recipients
            model.Recipients = recipients
                .GroupBy(r => r.Value)
                .Select(g => g.First())
                .OrderBy(r => r.Text)
                .ToList();

            return View(model);
        }


        // POST: /Messages/Create
        /// <summary>
        /// Handles the submission of the new conversation form.
        /// Validates the input, ensures the recipient list is repopulated if validation fails,
        /// and creates a new conversation and its first message in the system.
        /// </summary>
        /// <param name="model">The conversation creation form model containing subject, message, and recipient.</param>
        /// <returns>Redirects to the message index on success, or redisplays the form if validation fails.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Unit Owner, Condominium Manager, Company Administrator, Condominium Staff")]
        public async Task<IActionResult> Create(CreateConversationViewModel model)
        {
            ViewBag.CondominiumId = model.CondominiumId;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            // --- Re-populate the recipient list (same as GET) ---
            var recipients = new List<SelectListItem>();

            if (User.IsInRole("Company Administrator"))
            {
                if (model.CondominiumId.HasValue)
                {
                    var condo = await _context.Condominiums.FindAsync(model.CondominiumId.Value);
                    if (condo != null)
                    {
                        if (!string.IsNullOrEmpty(condo.CondominiumManagerId))
                        {
                            var manager = await _userManager.FindByIdAsync(condo.CondominiumManagerId);
                            if (manager != null)
                                recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
                        }

                        var staffInCondo = await _userRepository.GetStaffByCondominiumIdAsync(condo.Id);
                        recipients.AddRange(staffInCondo.Select(s =>
                            new SelectListItem($"{s.FirstName} {s.LastName} (Staff)", s.Id)));

                        var ownerIds = await _context.Units
                            .Where(u => u.CondominiumId == condo.Id && u.OwnerId != null)
                            .Select(u => u.OwnerId!)
                            .Distinct()
                            .ToListAsync();

                        var owners = await _userManager.Users
                            .Where(u => ownerIds.Contains(u.Id))
                            .ToListAsync();

                        recipients.AddRange(owners.Select(o =>
                            new SelectListItem($"{o.FirstName} {o.LastName} (Owner)", o.Id)));
                    }
                }
            }
            else if (User.IsInRole("Condominium Manager"))
            {
                var managedCondo = await _condominiumRepository.GetCondominiumByManagerIdAsync(currentUser.Id);
                if (managedCondo != null)
                {
                    var company = await _companyRepository.GetByIdAsync(managedCondo.CompanyId);
                    if (company?.ApplicationUserId is string adminId)
                    {
                        var admin = await _userManager.FindByIdAsync(adminId);
                        if (admin != null)
                            recipients.Add(new SelectListItem($"{admin.FirstName} {admin.LastName} (Admin)", admin.Id));
                    }

                    var staff = await _userRepository.GetStaffByCondominiumIdAsync(managedCondo.Id);
                    recipients.AddRange(staff.Select(s =>
                        new SelectListItem($"{s.FirstName} {s.LastName} (Staff)", s.Id)));

                    var ownerIds = await _context.Units
                        .Where(u => u.CondominiumId == managedCondo.Id && u.OwnerId != null)
                        .Select(u => u.OwnerId!)
                        .Distinct()
                        .ToListAsync();

                    var owners = await _userManager.Users
                        .Where(u => ownerIds.Contains(u.Id))
                        .ToListAsync();

                    recipients.AddRange(owners.Select(o =>
                        new SelectListItem($"{o.FirstName} {o.LastName} (Owner)", o.Id)));
                }
            }
            else if (User.IsInRole("Condominium Staff"))
            {
                if (currentUser.CondominiumId.HasValue)
                {
                    var condo = await _condominiumRepository.GetByIdAsync(currentUser.CondominiumId.Value);
                    if (condo != null)
                    {
                        if (!string.IsNullOrEmpty(condo.CondominiumManagerId))
                        {
                            var manager = await _userManager.FindByIdAsync(condo.CondominiumManagerId);
                            if (manager != null)
                                recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
                        }

                        var ownerIds = await _context.Units
                            .Where(u => u.CondominiumId == condo.Id && u.OwnerId != null)
                            .Select(u => u.OwnerId!)
                            .Distinct()
                            .ToListAsync();

                        var owners = await _userManager.Users
                            .Where(u => ownerIds.Contains(u.Id))
                            .ToListAsync();

                        recipients.AddRange(owners.Select(o =>
                            new SelectListItem($"{o.FirstName} {o.LastName} (Owner)", o.Id)));
                    }
                }
            }
            else if (User.IsInRole("Unit Owner"))
            {
                var assignedUnit = await _context.Units.FirstOrDefaultAsync(u => u.OwnerId == currentUser.Id);
                if (assignedUnit != null)
                {
                    var condo = await _context.Condominiums.FindAsync(assignedUnit.CondominiumId);
                    if (condo != null)
                    {
                        if (!string.IsNullOrEmpty(condo.CondominiumManagerId))
                        {
                            var manager = await _userManager.FindByIdAsync(condo.CondominiumManagerId);
                            if (manager != null)
                                recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
                        }

                        var company = await _companyRepository.GetByIdAsync(condo.CompanyId);
                        if (company?.ApplicationUserId is string adminId)
                        {
                            var admin = await _userManager.FindByIdAsync(adminId);
                            if (admin != null)
                                recipients.Add(new SelectListItem($"{admin.FirstName} {admin.LastName} (Admin)", admin.Id));
                        }
                    }
                }
            }

            model.Recipients = recipients;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // --- Role-aware Unit resolution ---
            Unit? unit = null;

            if (User.IsInRole("Unit Owner"))
            {
                unit = await _context.Units.FirstOrDefaultAsync(u => u.OwnerId == currentUser.Id);
                if (unit == null)
                {
                    ModelState.AddModelError("", "You need an assigned unit to start a conversation.");
                    return View(model);
                }
            }
            else
            {
                if (!model.CondominiumId.HasValue)
                {
                    ModelState.AddModelError("", "Missing condominium context.");
                    return View(model);
                }

                // If recipient is an Owner, tie to their unit
                var recipient = await _userManager.FindByIdAsync(model.RecipientId);
                bool recipientIsOwner = recipient != null && await _userManager.IsInRoleAsync(recipient, "Unit Owner");

                if (recipientIsOwner)
                {
                    unit = await _context.Units.FirstOrDefaultAsync(u =>
                        u.CondominiumId == model.CondominiumId.Value && u.OwnerId == model.RecipientId);
                }

                // Otherwise just grab any existing unit in the condo
                if (unit == null)
                {
                    unit = await _context.Units
                        .Where(u => u.CondominiumId == model.CondominiumId.Value)
                        .OrderBy(u => u.Id)
                        .FirstOrDefaultAsync();
                }

                if (unit == null)
                {
                    ModelState.AddModelError("", "This condominium has no units configured yet.");
                    return View(model);
                }
            }

            // --- Create Conversation ---
            var conversation = new Conversation
            {
                Subject = model.Subject,
                InitiatorId = currentUser.Id,
                AssignedToId = model.RecipientId,
                UnitId = unit.Id, // ✅ now always a real unit
                Status = MessageStatus.Pending
            };
            _context.Conversations.Add(conversation);
            await _context.SaveChangesAsync();

            // --- Create first Message ---
            var message = new Message
            {
                Content = model.Message,
                SenderId = currentUser.Id,
                ReceiverId = model.RecipientId,
                ConversationId = conversation.Id,
            };
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // TODO: Use SignalR to notify the specific recipient in real-time.

            TempData["StatusMessage"] = "Your new conversation has been created successfully.";
            return RedirectToAction(nameof(Index));
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