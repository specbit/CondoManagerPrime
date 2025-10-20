using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Data.Entities;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Enums;
using CET96_ProjetoFinal.web.Hubs;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.SignalR;
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
        private readonly IHubContext<ChatHub> _hub;

        public MessagesController(
            CondominiumDataContext context,
            UserManager<ApplicationUser> userManager,
            ICompanyRepository companyRepository,
            IApplicationUserRepository userRepository,
            ICondominiumRepository condominiumRepository,
            IHubContext<ChatHub> hub)
        {
            _context = context;
            _userManager = userManager;
            _companyRepository = companyRepository;
            _userRepository = userRepository;
            _condominiumRepository = condominiumRepository;
            _hub = hub;
        }

        // The main action to display the messaging page

        /// <summary>
        /// Displays the main message center view for the current user.
        /// </summary>
        /// <param name="condominiumId">
        /// The unique identifier of the condominium context. If provided and the current user
        /// is a <c>Condominium Manager</c> or <c>Company Administrator</c>, all conversations
        /// for that condominium are displayed. Otherwise, only conversations in which the
        /// current user is a participant (initiator or assignee) are shown.
        /// </param>
        /// <returns>
        /// A view containing a list of conversations represented by <see cref="ConversationViewModel"/>.
        /// Each conversation includes subject, status, participants, roles, unit information,
        /// assignment details, initiator metadata, and a <c>CanAssign</c> policy flag to control
        /// whether the Assign button is rendered in the UI.
        /// </returns>
        /// <remarks>
        /// <para><strong>Role-based behavior:</strong></para>
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>Manager/Admin</c>: Can view all conversations for the specified condominium.</description>
        ///   </item>
        ///   <item>
        ///     <description><c>Staff/Owner</c>: Only sees conversations they initiated or were assigned to.</description>
        ///   </item>
        /// </list>
        ///
        /// <para><strong>Included metadata:</strong></para>
        /// <list type="bullet">
        ///   <item><description><c>StarterName</c> and <c>StarterRole</c>: Identify who started the conversation.</description></item>
        ///   <item><description><c>AssignedToName</c> and <c>AssignedToRole</c>: Show current assignee details.</description></item>
        ///   <item><description><c>UnitNumber</c>: Displays the unit number if applicable (owners only).</description></item>
        ///   <item><description><c>StatusCss</c>: A precomputed CSS class for rendering the status indicator dot.</description></item>
        ///   <item><description><c>UnreadCount</c>: The number of unread messages in the conversation for the current user. If greater than zero, a red badge is displayed next to the subject.</description></item>
        ///   <item><description><c>CanAssign</c>: Indicates whether the current user can assign or reassign the conversation.</description></item>
        /// </list>
        ///
        /// <para><strong>UI behavior:</strong></para>
        /// - Conversations with unread messages show a red badge (<c>bg-danger</c>) next to the subject.
        /// - Status dots and labels update dynamically via SignalR when conversation state changes.
        /// - The Assign button is hidden if <c>CanAssign</c> is <c>false</c>.
        /// </remarks>
        public async Task<IActionResult> Index(int? condominiumId, bool assignedByMe = false)
        {
            ViewBag.CondominiumId = condominiumId;


            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 1) Conversations - if Manager/Admin, show all for the condominium
            IQueryable<Conversation> q = _context.Conversations;

            var isMgrOrAdmin = User.IsInRole("Condominium Manager") || User.IsInRole("Company Administrator");
            if (isMgrOrAdmin && condominiumId.HasValue)
            {
                var condo = condominiumId.Value;
                q = q.Where(c => _context.Units.Any(u => u.Id == c.UnitId && u.CondominiumId == condo));
            }
            else
            {
                q = q.Where(c => c.InitiatorId == currentUserId || c.AssignedToId == currentUserId);
            }

            // ✅ Managers: also keep track of threads they assigned to staff (when not scoped to a specific condominium)
            //    We piggyback on the existing system note format: "[System] Assigned to ..."
            if (User.IsInRole("Condominium Manager") && !User.IsInRole("Company Administrator") && !condominiumId.HasValue)
            {
                var myAssignedConvoIds = await _context.Messages
                    .Where(m => m.SenderId == currentUserId
                             && m.ConversationId != 0
                             && m.Content.StartsWith("[System] Assigned to"))
                    .Select(m => m.ConversationId)
                    .Distinct()
                    .ToListAsync();

                // widen the scope to include assignments made by this manager
                q = _context.Conversations.Where(c =>
                    c.InitiatorId == currentUserId ||
                    c.AssignedToId == currentUserId ||
                    myAssignedConvoIds.Contains(c.Id));
            }

            var conversations = await q
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            // --- Unread counts for the current user across those conversations
            var convoIds = conversations.Select(c => c.Id).ToList();
            var unreadCounts = await _context.Messages
                .Where(m => convoIds.Contains(m.ConversationId)
                         && m.ReceiverId == currentUserId
                         && !m.IsRead)
                .GroupBy(m => m.ConversationId)
                .Select(g => new { ConversationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ConversationId, x => x.Count);

            // 1a) Units
            var unitIds = conversations
                .Select(c => c.UnitId)
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            var unitNumbersById = await _context.Units
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.UnitNumber);

            // 2) User IDs (initiators, assignees) — include both to resolve roles & names in one pass
            var userIds = conversations
                .SelectMany(c => new[] { c.InitiatorId, c.AssignedToId })
                .Where(id => id != null)
                .Distinct()
                .ToList();

            // 3) Users
            var users = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            // 3a) Roles
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

            // Who am I (policy flags)?
            bool isAdmin = User.IsInRole("Company Administrator");
            bool isManager = User.IsInRole("Condominium Manager");
            bool isStaff = User.IsInRole("Condominium Staff");

            // 4) Project to ViewModel
            var model = conversations.Select(c =>
            {
                // Helper for nice display names
                string DisplayName(ApplicationUser? u) =>
                    (u is null) ? "System" :
                    $"{(u.FirstName ?? "").Trim()} {(u.LastName ?? "").Trim()}".Trim() switch
                    {
                        "" => (u.Email ?? u.UserName ?? "User"),
                        var s => s
                    };

                // Other participant (for display)
                var otherUserId = c.InitiatorId == currentUserId ? c.AssignedToId : c.InitiatorId;
                users.TryGetValue(otherUserId ?? "", out var otherUser);

                var (otherRole, otherBadge) = (otherUserId != null && rolesByUser.TryGetValue(otherUserId, out var tuple))
                    ? tuple : ("User", "bg-secondary");

                // Assigned user (for "Assignee")
                users.TryGetValue(c.AssignedToId ?? "", out var assignedUser);
                var assignedRole = (c.AssignedToId != null && rolesByUser.TryGetValue(c.AssignedToId, out var aTuple))
                    ? aTuple.Role : null;

                // Initiator (STARTER)
                users.TryGetValue(c.InitiatorId, out var initiatorUser);
                var starterName = DisplayName(initiatorUser);
                var starterRole = rolesByUser.TryGetValue(c.InitiatorId, out var initTupleRole)
                    ? initTupleRole.Role
                    : "User";

                var initiatorRole = starterRole;

                // Status -> css token
                var statusCss = c.Status switch
                {
                    MessageStatus.Pending => "status-pending",
                    MessageStatus.Assigned => "status-assigned",
                    MessageStatus.InProgress => "status-inprogress",
                    MessageStatus.Resolved => "status-resolved",
                    MessageStatus.Closed => "status-closed",
                    _ => "status-closed"
                };

                var unitNumber = unitNumbersById.TryGetValue(c.UnitId, out var num) ? num : null;

                // CanAssign policy (matches your GET/POST guards)
                // - Only when Pending (not Assigned/InProgress/Resolved/Closed)
                // - Admin: can assign any pending thread
                // - Manager: can assign pending threads only if initiator is Unit Owner or Condominium Staff
                bool canAssign =
                    c.Status == MessageStatus.Pending && (
                        isAdmin ||
                        (isManager && (initiatorRole == "Unit Owner" || initiatorRole == "Condominium Staff"))
                    );

                // --- Per-conversation workflow permissions
                bool amAssignedStaff = isStaff && c.AssignedToId == currentUserId;
                bool canMarkInProgress =
                    c.Status == MessageStatus.Assigned && (amAssignedStaff || isManager || isAdmin);

                // Manager/Admin may resolve directly from Assigned (skip InProgress if they want)
                bool canResolve =
                    c.Status == MessageStatus.InProgress && (amAssignedStaff || isManager || isAdmin);

                bool canClose =
                    c.Status == MessageStatus.Resolved && (isManager || isAdmin);

                // --- Unread count for this conversation
                int unread = unreadCounts.TryGetValue(c.Id, out var cnt) ? cnt : 0;

                return new ConversationViewModel
                {
                    Id = c.Id,
                    Subject = c.Subject,
                    Status = c.Status.ToString(),
                    CreatedAt = c.CreatedAt,

                    // Show who started the thread
                    StarterName = starterName,
                    StarterRole = starterRole,

                    // Keep “other participant” for any places you still use it
                    OtherParticipantName = DisplayName(otherUser),
                    OtherParticipantRole = otherRole,
                    OtherRoleBadgeCss = otherBadge,

                    StatusCss = statusCss,
                    UnitNumber = otherRole == "Unit Owner" ? unitNumber : null,

                    // Assignment info (rendered as "Assignee:")
                    AssignedToName = assignedUser != null ? DisplayName(assignedUser) : null,
                    AssignedToRole = assignedRole,

                    // Metadata for UI policy & trace
                    InitiatorRole = initiatorRole,
                    CanAssign = canAssign,

                    UnreadCount = unread,
                    CanMarkInProgress = canMarkInProgress,
                    CanResolve = canResolve,
                    CanClose = canClose
                };
            }).ToList();

            // Bubble up any GET-guard error message (so the view can show it once)
            ViewBag.Error = TempData["Error"] as string;

            return View(model);
        }



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

                    // --- owner ids first (so we can label owner even if they’re also staff)
                    var ownerIds = await _context.Units
                        .Where(u => u.CondominiumId == managedCondo.Id && u.OwnerId != null)
                        .Select(u => u.OwnerId!)
                        .Distinct()
                        .ToListAsync();
                    var ownerIdSet = ownerIds.ToHashSet();

                    // Staff (EXCLUDING anyone who is an Owner)
                    var staff = await _userRepository.GetStaffByCondominiumIdAsync(managedCondo.Id);
                    foreach (var s in staff.Where(s => !ownerIdSet.Contains(s.Id)))
                    {
                        recipients.Add(new SelectListItem($"{s.FirstName} {s.LastName} (Staff)", s.Id));
                    }

                    // Owners
                    var owners = await _userManager.Users
                        .Where(u => ownerIdSet.Contains(u.Id))
                        .ToListAsync();

                    foreach (var o in owners)
                    {
                        recipients.Add(new SelectListItem($"{o.FirstName} {o.LastName} (Owner)", o.Id));
                    }
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

                    // Owner ids first
                    var ownerIds = await _context.Units
                        .Where(u => u.CondominiumId == managedCondo.Id && u.OwnerId != null)
                        .Select(u => u.OwnerId!)
                        .Distinct()
                        .ToListAsync();
                    var ownerIdSet = ownerIds.ToHashSet();

                    // Staff (exclude owners)
                    var staff = await _userRepository.GetStaffByCondominiumIdAsync(managedCondo.Id);
                    foreach (var s in staff.Where(s => !ownerIdSet.Contains(s.Id)))
                    {
                        recipients.Add(new SelectListItem($"{s.FirstName} {s.LastName} (Staff)", s.Id));
                    }

                    // Owners
                    var owners = await _userManager.Users
                        .Where(u => ownerIdSet.Contains(u.Id))
                        .ToListAsync();

                    foreach (var o in owners)
                    {
                        recipients.Add(new SelectListItem($"{o.FirstName} {o.LastName} (Owner)", o.Id));
                    }
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

            // ✅ Mark messages as read for the current user (so unread badge disappears)
            var currentUserId = _userManager.GetUserId(User);
            var unreadMessages = messages
                .Where(m => m.ReceiverId == currentUserId && !m.IsRead)
                .ToList();

            if (unreadMessages.Any())
            {
                foreach (var msg in unreadMessages)
                {
                    msg.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

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


        /// <summary>
        /// Displays a form that allows a manager or company administrator to assign a conversation 
        /// to an eligible staff member for handling.
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the conversation to be assigned.
        /// </param>
        /// <returns>
        /// A view containing a dropdown list of active staff members associated with the same 
        /// condominium as the conversation. Only active staff are included.
        /// </returns>
        /// <remarks>
        /// Access rules:
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>Company Administrator</c>: may assign any conversation.</description>
        ///   </item>
        ///   <item>
        ///     <description><c>Condominium Manager</c>: may assign only conversations started by a 
        ///     <c>Unit Owner</c> or <c>Condominium Staff</c>. Attempts to assign manager/admin-initiated
        ///     threads are rejected and redirected back to the index with an error message.</description>
        ///   </item>
        /// </list>
        /// The staff list is filtered by role (<c>Condominium Staff</c>), activation status
        /// (<c>DeactivatedAt == null</c>), and condominium association.
        /// 
        /// UI note: the assignee should be displayed with the label <c>Assignee:</c> on the card/list
        /// (avoid an email-style <c>To:</c> label to prevent ambiguity).
        /// </remarks>
        [HttpGet]
        [Authorize(Roles = "Condominium Manager, Company Administrator")]
        public async Task<IActionResult> AssignToStaff(int id)
        {
            // 1) Load conversation (NO Include — Unit is [NotMapped])
            var convo = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == id);

            if (convo == null) return NotFound();

            // Load Unit to get condo id
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == convo.UnitId);
            if (unit == null) return NotFound();

            var condoId = unit.CondominiumId;  // int

            // --- Managers may assign only Owner/Staff-initiated threads; Admins can assign anything.
            var isManagerOnly = User.IsInRole("Condominium Manager") && !User.IsInRole("Company Administrator");
            if (isManagerOnly)
            {
                var initiator = await _userManager.FindByIdAsync(convo.InitiatorId);
                var initiatorIsOwner = initiator != null && await _userManager.IsInRoleAsync(initiator, "Unit Owner");
                var initiatorIsStaff = initiator != null && await _userManager.IsInRoleAsync(initiator, "Condominium Staff");

                if (!(initiatorIsOwner || initiatorIsStaff))
                {
                    TempData["Error"] = "Managers can only assign conversations started by a Unit Owner or Condominium Staff.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // 2) Get all users in the STAFF role
            var allStaff = await _userManager.GetUsersInRoleAsync("Condominium Staff"); // change role name if needed

            // 3) Filter to ACTIVE and SAME CONDO
            var eligibleStaff = allStaff
                .Where(u => u.DeactivatedAt == null                      // active only
                         && u.CondominiumId.HasValue
                         && u.CondominiumId.Value == condoId)            // same condominium
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToList();

            ViewBag.ConversationId = convo.Id;
            ViewBag.CurrentAssigneeId = convo.AssignedToId;
            ViewBag.Staff = eligibleStaff
                .Select(u => new SelectListItem(
                    $"{u.FirstName} {u.LastName}",
                    u.Id,
                    convo.AssignedToId == u.Id))
                .ToList();

            return View();
        }

        /// <summary>
        /// Assigns an existing conversation to a specific staff member for handling and 
        /// updates its workflow status to <see cref="MessageStatus.Assigned"/>.
        /// </summary>
        /// <param name="conversationId">
        /// The unique identifier of the conversation to assign.
        /// </param>
        /// <param name="staffUserId">
        /// The ID of the staff user who will handle the conversation.
        /// </param>
        /// <returns>
        /// A redirect to the conversation list (or details page) after the assignment is complete.
        /// </returns>
        /// <remarks>
        /// Access rules:
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>Company Administrator</c>: may assign any conversation.</description>
        ///   </item>
        ///   <item>
        ///     <description><c>Condominium Manager</c>: may only assign conversations started by a 
        ///     <c>Unit Owner</c> or <c>Condominium Staff</c>. Attempts to assign conversations initiated 
        ///     by another manager or administrator are blocked and redirected with an error message.</description>
        ///   </item>
        /// </list>
        /// This action also appends a system-generated message to the conversation thread (audit trail),
        /// and broadcasts a real-time update via SignalR so the UI can refresh the card showing
        /// <c>Assignee:</c> and status without relying on an email-style <c>To:</c> field.
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Condominium Manager, Company Administrator")]
        public async Task<IActionResult> AssignToStaff(int conversationId, string staffUserId)
        {
            // 1) Load conversation (NO Include — Unit is [NotMapped])
            var convo = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);
            if (convo == null) return NotFound();

            // 2) Basic guard: don't assign closed threads
            if (convo.Status == MessageStatus.Closed)
            {
                TempData["Error"] = "This conversation is already closed.";
                return RedirectToAction(nameof(Index));
            }

            // 3) Validate staff user exists
            var staff = await _userManager.FindByIdAsync(staffUserId);
            if (staff == null)
            {
                TempData["Error"] = "Selected staff member not found.";
                return RedirectToAction(nameof(Index));
            }

            // 4) Verify that the selected user is eligible to receive assignments
            // Load Unit because convo.Unit is [NotMapped]
            var unit = await _context.Units.FirstOrDefaultAsync(u => u.Id == convo.UnitId);

            var inStaffRole = await _userManager.IsInRoleAsync(staff, "Condominium Staff");
            var isActive = staff.DeactivatedAt == null;
            var sameCondo = unit != null
                              && staff.CondominiumId.HasValue
                              && staff.CondominiumId.Value == unit.CondominiumId;

            if (!inStaffRole || !isActive || !sameCondo)
            {
                TempData["Error"] = "The selected user is not an eligible active staff member for this condominium.";
                return RedirectToAction(nameof(AssignToStaff), new { id = conversationId });
            }

            // Managers may assign only conversations started by an Owner or Staff.
            // Company Admins can assign anything.
            var isManager = User.IsInRole("Condominium Manager") && !User.IsInRole("Company Administrator");
            if (isManager)
            {
                var initiator = await _userManager.FindByIdAsync(convo.InitiatorId);
                var initiatorIsOwner = initiator != null && await _userManager.IsInRoleAsync(initiator, "Unit Owner");
                var initiatorIsStaff = initiator != null && await _userManager.IsInRoleAsync(initiator, "Condominium Staff");

                if (!(initiatorIsOwner || initiatorIsStaff))
                {
                    TempData["Error"] = "Managers can assign only conversations started by a Unit Owner or Condominium Staff.";
                    return RedirectToAction(nameof(Index));
                }
            }

            // 5) Update conversation state
            convo.AssignedToId = staff.Id;
            convo.Status = MessageStatus.Assigned; // UI should display as "Assignee: {Name}" (not "To:")
            await _context.SaveChangesAsync();

            // --- Real-time notify the conversation group (SignalR) ---
            // Keep payload keys stable; the UI will use these to update the card.
            string assigneeName = $"{staff.FirstName} {staff.LastName}".Trim();
            try
            {
                await _hub.Clients
                    .Group($"conversation-{convo.Id}")
                    .SendAsync("ConversationUpdated", new
                    {
                        conversationId = convo.Id,
                        status = convo.Status.ToString(),
                        assignedToName = assigneeName,
                        assignedToRole = "Condominium Staff" // or resolve actual role if you support multiple
                    });
            }
            catch
            {
                // If SignalR isn't configured/connected, just ignore; the core flow still works.
            }

            // 6) Append a simple “[System] …” message for traceability (no schema changes)
            var actorId = _userManager.GetUserId(User) ?? "system";
            var actor = await _userManager.FindByIdAsync(actorId);

            string ShowName(IdentityUser? u) =>
                (u as ApplicationUser)?.FirstName is string fn && (u as ApplicationUser)?.LastName is string ln
                    ? $"{fn} {ln}"
                    : u?.Email ?? u?.UserName ?? u?.Id ?? "User";

            var actorName = ShowName(actor);

            _context.Messages.Add(new Message
            {
                ConversationId = convo.Id,
                SenderId = actorId,
                ReceiverId = staff.Id, // informational; your UI doesn’t rely on this
                Content = $"[System] Assigned to {assigneeName} by {actorName} ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC)",
                SentAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Conversation assigned to staff.";
            return RedirectToAction(nameof(Index)); // change to Details if you have it
        }


        /// <summary>
        /// Marks a conversation as <see cref="MessageStatus.InProgress"/> to indicate work has started.
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the conversation to update.
        /// </param>
        /// <returns>
        /// A redirect to <see cref="Index"/> after the status is updated (or to <see cref="Index"/> with an error if not permitted).
        /// </returns>
        /// <remarks>
        /// <para><strong>Access control</strong></para>
        /// Only the assigned <c>Condominium Staff</c> member may mark the conversation In&nbsp;Progress,
        /// or a <c>Condominium Manager</c>/<c>Company Administrator</c>.
        ///
        /// <para><strong>Behavior</strong></para>
        /// <list type="bullet">
        ///   <item><description>Returns 404 if the conversation does not exist.</description></item>
        ///   <item><description>Refuses changes to conversations already in <c>Closed</c> state.</description></item>
        ///   <item><description>Validates the caller is either the assigned staff member or a manager/admin.</description></item>
        ///   <item><description>Sets the status to <c>InProgress</c> and appends a system message noting who made the change and when.</description></item>
        ///   <item><description>Shows a success banner via <c>TempData["StatusMessage"]</c> on redirect.</description></item>
        /// </list>
        /// </remarks>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Condominium Staff, Condominium Manager, Company Administrator")]
        public async Task<IActionResult> MarkInProgress(int id)
        {
            var convo = await _context.Conversations.FirstOrDefaultAsync(c => c.Id == id);
            if (convo == null) return NotFound();

            // Block if already closed
            if (convo.Status == MessageStatus.Closed)
            {
                TempData["Error"] = "Cannot change status of a closed conversation.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User)!;

            // Only assigned staff OR manager/admin may move to InProgress
            var isManagerOrAdmin = User.IsInRole("Condominium Manager") || User.IsInRole("Company Administrator");
            var isAssignedStaff = convo.AssignedToId == userId && User.IsInRole("Condominium Staff");
            if (!isAssignedStaff && !isManagerOrAdmin)
            {
                return Forbid();
            }

            // Set status
            convo.Status = MessageStatus.InProgress;

            // System note
            var actor = await _userManager.FindByIdAsync(userId);
            var actorName = (actor?.FirstName, actor?.LastName) is (string fn, string ln) ? $"{fn} {ln}"
                         : actor?.Email ?? actor?.UserName ?? "User";

            _context.Messages.Add(new Message
            {
                ConversationId = convo.Id,
                SenderId = userId,
                ReceiverId = null,
                Content = $"[System] Marked as In Progress by {actorName} ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC)",
                SentAt = DateTime.UtcNow,
                IsRead = false
            });

            await _context.SaveChangesAsync();

            TempData["StatusMessage"] = "Conversation marked as In Progress.";
            return RedirectToAction(nameof(Index));
        }


        /// <summary>
        /// Marks a conversation as <see cref="MessageStatus.Resolved"/> to indicate that the reported issue
        /// has been addressed by staff or management.
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the conversation to mark as resolved.
        /// </param>
        /// <returns>
        /// A redirect to the <see cref="Index"/> view after successfully updating the conversation status.
        /// </returns>
        /// <remarks>
        /// <para><strong>Access Control:</strong></para>
        /// This action is restricted to:
        /// <list type="bullet">
        ///   <item><description>Condominium Staff — only if they are the assigned handler of the conversation.</description></item>
        ///   <item><description>Condominium Manager and Company Administrator — unrestricted for conversations in their scope.</description></item>
        /// </list>
        ///
        /// <para><strong>Behavior:</strong></para>
        /// <list type="bullet">
        ///   <item><description>Validates that the conversation exists and is not already <c>Closed</c>.</description></item>
        ///   <item><description>Checks authorization to ensure that only the assigned staff or a manager/admin can perform this action.</description></item>
        ///   <item><description>Updates the conversation’s status to <c>Resolved</c> in the database.</description></item>
        ///   <item><description>Appends a system-generated message to the conversation indicating who marked it resolved and when.</description></item>
        ///   <item><description>Broadcasts a <c>ConversationUpdated</c> SignalR event to all connected clients in the conversation group to trigger UI updates (e.g., status label, buttons).</description></item>
        /// </list>
        ///
        /// <para><strong>UI Impact:</strong></para>
        /// - The conversation card updates to show a <c>Resolved</c> status badge and status dot.
        /// - The “Mark Resolved” button should disappear or become disabled after this action.
        /// - Managers can later choose to close the conversation.
        /// </remarks>

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Condominium Staff, Condominium Manager, Company Administrator")]
        public async Task<IActionResult> MarkResolved(int id)
        {
            var convo = await _context.Conversations.FirstOrDefaultAsync(c => c.Id == id);
            if (convo == null) return NotFound();
            if (convo.Status == MessageStatus.Closed)
            {
                TempData["Error"] = "Conversation already closed.";
                return RedirectToAction(nameof(Index));
            }

            var currentUserId = _userManager.GetUserId(User)!;
            var isManagerOrAdmin = User.IsInRole("Condominium Manager") || User.IsInRole("Company Administrator");
            var isAssignedStaff = convo.AssignedToId == currentUserId && User.IsInRole("Condominium Staff");
            if (!isAssignedStaff && !isManagerOrAdmin) return Forbid();

            convo.Status = MessageStatus.Resolved;

            // system note
            var actor = await _userManager.FindByIdAsync(currentUserId);
            var actorName = $"{actor?.FirstName} {actor?.LastName}".Trim();
            _context.Messages.Add(new Message
            {
                ConversationId = convo.Id,
                SenderId = currentUserId,
                Content = $"[System] Marked as Resolved by {actorName} ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC)",
                SentAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            try
            {
                await _hub.Clients.Group($"conversation-{convo.Id}")
                    .SendAsync("ConversationUpdated", new
                    {
                        conversationId = convo.Id,
                        status = convo.Status.ToString()
                    });
            }
            catch { }

            TempData["StatusMessage"] = "Conversation marked as Resolved.";
            return RedirectToAction(nameof(Index));
        }


        /// <summary>
        /// Closes an existing conversation by updating its status to <see cref="MessageStatus.Closed"/>.
        /// </summary>
        /// <param name="id">
        /// The unique identifier of the conversation to close.
        /// </param>
        /// <returns>
        /// A redirect to the <see cref="Index"/> view after successfully closing the conversation.
        /// </returns>
        /// <remarks>
        /// <para><strong>Access Control:</strong></para>
        /// Only users in the <c>Condominium Manager</c> or <c>Company Administrator</c> roles are authorized to close a conversation.
        ///
        /// <para><strong>Behavior:</strong></para>
        /// <list type="bullet">
        ///   <item>
        ///     <description>Updates the conversation’s status to <c>Closed</c> in the database.</description>
        ///   </item>
        ///   <item>
        ///     <description>Appends a system-generated message to the conversation thread indicating who closed it and when.</description>
        ///   </item>
        ///   <item>
        ///     <description>Broadcasts a <c>ConversationUpdated</c> SignalR event to all connected clients in the conversation group so the UI can update in real time (e.g., status badges, buttons).</description>
        ///   </item>
        ///   <item>
        ///     <description>After completion, redirects the user to the <c>Index</c> view and displays a status message.</description>
        ///   </item>
        /// </list>
        ///
        /// <para><strong>UI Impact:</strong></para>
        /// - The conversation will show a <c>Closed</c> status label and status dot.
        /// - Conversation input is expected to be disabled in the client UI once closed.
        /// - The Assign and workflow buttons are hidden once the status becomes <c>Closed</c>.
        /// </remarks>

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Condominium Manager, Company Administrator")]
        public async Task<IActionResult> Close(int id)
        {
            var convo = await _context.Conversations.FirstOrDefaultAsync(c => c.Id == id);
            if (convo == null) return NotFound();

            convo.Status = MessageStatus.Closed;

            var currentUserId = _userManager.GetUserId(User)!;
            var actor = await _userManager.FindByIdAsync(currentUserId);
            var actorName = $"{actor?.FirstName} {actor?.LastName}".Trim();

            _context.Messages.Add(new Message
            {
                ConversationId = convo.Id,
                SenderId = currentUserId,
                Content = $"[System] Closed by {actorName} ({DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC)",
                SentAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            try
            {
                await _hub.Clients.Group($"conversation-{convo.Id}")
                    .SendAsync("ConversationUpdated", new
                    {
                        conversationId = convo.Id,
                        status = convo.Status.ToString()
                    });
            }
            catch { }

            TempData["StatusMessage"] = "Conversation closed.";
            return RedirectToAction(nameof(Index));
        }
    }
}