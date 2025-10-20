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
        /// A view containing a list of conversations represented by <see cref="ConversationViewModel"/>
        /// objects, including metadata such as subject, status, participants, unit, assignee details,
        /// the initiator’s role, and a precomputed <c>CanAssign</c> flag (so the view can hide the
        /// Assign button when policy forbids it).
        /// </returns>
        /// <remarks>
        /// This action dynamically adjusts the conversation list based on the current user's role:
        /// <list type="bullet">
        ///   <item>
        ///     <description><c>Manager/Admin</c>: Can view all conversations for the specified condominium.</description>
        ///   </item>
        ///   <item>
        ///     <description><c>Staff/Owner</c>: Only sees conversations they initiated or were assigned to.</description>
        ///   </item>
        /// </list>
        /// Additionally, the result includes assignment metadata (<c>AssignedToName</c>, <c>AssignedToRole</c>),
        /// and the <c>InitiatorRole</c> + <c>CanAssign</c> policy flag used by the UI.
        /// </remarks>
        public async Task<IActionResult> Index(int? condominiumId)
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

            var conversations = await q
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

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

                // Initiator (STARTER) — NEW: populate StarterName / StarterRole
                users.TryGetValue(c.InitiatorId, out var initiatorUser);
                var starterName = DisplayName(initiatorUser);
                var starterRole = rolesByUser.TryGetValue(c.InitiatorId, out var initTupleRole)
                    ? initTupleRole.Role
                    : "User";

                // Initiator role (used by CanAssign policy) — keep existing semantics
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
                // - Admin: can assign any (except Closed)
                // - Manager: can assign only if initiator is Owner or Staff (and not Closed)
                bool canAssign =
                    c.Status != MessageStatus.Closed && (
                        isAdmin ||
                        (isManager && (initiatorRole == "Unit Owner" || initiatorRole == "Condominium Staff"))
                    );

                return new ConversationViewModel
                {
                    Id = c.Id,
                    Subject = c.Subject,
                    Status = c.Status.ToString(),
                    CreatedAt = c.CreatedAt,

                    // NEW: show who started the thread
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

                    // NEW metadata for UI policy & trace
                    InitiatorRole = initiatorRole,
                    CanAssign = canAssign
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


        // TODO: delete this commented-out GET action if not needed

        // GET: /Messages/Create
        ///// <summary>
        ///// Displays the form for a user to create a new conversation.
        ///// It intelligently populates the recipient list based on the user's role.
        ///// </summary>
        //[Authorize(Roles = "Unit Owner, Condominium Manager, Company Administrator, Condominium Staff")]
        //[HttpGet]
        //public async Task<IActionResult> Create(int? condominiumId, string? recipientId = null)
        //{
        //    ViewBag.CondominiumId = condominiumId; // For return url

        //    var model = new CreateConversationViewModel
        //    {
        //        RecipientId = recipientId
        //    };
        //    var currentUser = await _userManager.GetUserAsync(User);
        //    if (currentUser == null) return Unauthorized();

        //    var recipients = new List<SelectListItem>();

        //    // --- Smart Recipient Logic ---
        //    if (User.IsInRole("Unit Owner"))
        //    {
        //        // An Owner can message their Manager and the Company Admin.
        //        var assignedUnit = await _context.Units.FirstOrDefaultAsync(u => u.OwnerId == currentUser.Id);
        //        if (assignedUnit != null)
        //        {
        //            var condominium = await _context.Condominiums.FindAsync(assignedUnit.CondominiumId);
        //            if (condominium != null)
        //            {
        //                // Add the Condo Manager
        //                if (!string.IsNullOrEmpty(condominium.CondominiumManagerId))
        //                {
        //                    var manager = await _userManager.FindByIdAsync(condominium.CondominiumManagerId);
        //                    if (manager != null)
        //                        recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
        //                }
        //                // Add the Company Admin
        //                var company = await _companyRepository.GetByIdAsync(condominium.CompanyId);
        //                if (company != null && !string.IsNullOrEmpty(company.ApplicationUserId))
        //                {
        //                    var admin = await _userManager.FindByIdAsync(company.ApplicationUserId);
        //                    if (admin != null)
        //                        recipients.Add(new SelectListItem($"{admin.FirstName} {admin.LastName} (Admin)", admin.Id));
        //                }
        //            }
        //        }
        //    }
        //    else if (User.IsInRole("Company Administrator"))
        //    {
        //        // A Company Admin can message all the managers in their company.
        //        var allManagers = await _userManager.GetUsersInRoleAsync("Condominium Manager");
        //        var managersInCompany = allManagers.Where(m => m.CompanyId == currentUser.CompanyId);

        //        foreach (var manager in managersInCompany)
        //        {
        //            recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
        //        }
        //    }
        //    else if (User.IsInRole("Condominium Manager"))
        //    {
        //        // A Manager can message their Staff, Owners in their condo, and their Company Admin.
        //        var managedCondo = await _condominiumRepository.GetCondominiumByManagerIdAsync(currentUser.Id);
        //        if (managedCondo != null)
        //        {
        //            // Add their Company Admin
        //            var company = await _companyRepository.GetByIdAsync(managedCondo.CompanyId);
        //            if (company != null && !string.IsNullOrEmpty(company.ApplicationUserId))
        //            {
        //                var admin = await _userManager.FindByIdAsync(company.ApplicationUserId);
        //                if (admin != null && admin.Id != currentUser.Id)
        //                    recipients.Add(new SelectListItem($"{admin.FirstName} {admin.LastName} (Admin)", admin.Id));
        //            }
        //            // Add all Staff in their condo
        //            var staffInCondo = await _userRepository.GetStaffByCondominiumIdAsync(managedCondo.Id);
        //            foreach (var staff in staffInCondo)
        //            {
        //                recipients.Add(new SelectListItem($"{staff.FirstName} {staff.LastName} (Staff)", staff.Id));
        //            }

        //            // Add all Owners in their condo
        //            var ownersInCondo = await _userRepository.GetUsersInRoleByCondominiumAsync("Unit Owner", managedCondo.Id);

        //            foreach (var owner in ownersInCondo)
        //            {
        //                recipients.Add(new SelectListItem($"{owner.FirstName} {owner.LastName} (Owner)", owner.Id));
        //            }
        //        }
        //    }
        //    else if (User.IsInRole("Condominium Staff"))
        //    {
        //        // A Staff member can message their Manager and Owners in their condo.
        //        if (currentUser.CondominiumId.HasValue)
        //        {
        //            var assignedCondo = await _condominiumRepository.GetByIdAsync(currentUser.CondominiumId.Value);
        //            if (assignedCondo != null)
        //            {
        //                // Add their Condo Manager
        //                if (!string.IsNullOrEmpty(assignedCondo.CondominiumManagerId))
        //                {
        //                    var manager = await _userManager.FindByIdAsync(assignedCondo.CondominiumManagerId);
        //                    if (manager != null)
        //                        recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
        //                }
        //                // Add all Owners in their condo
        //                var ownersInCondo = await _userRepository.GetUsersInRoleByCondominiumAsync("Unit Owner", assignedCondo.Id);
        //                foreach (var owner in ownersInCondo)
        //                {
        //                    recipients.Add(new SelectListItem($"{owner.FirstName} {owner.LastName} (Owner)", owner.Id));
        //                }
        //            }
        //        }
        //    }

        //    model.Recipients = recipients.OrderBy(r => r.Text);
        //    return View(model);
        //}

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



        // TODO: delete if upgrage works

        ///// <summary>
        ///// Handles the submission of the new conversation form.
        ///// </summary>
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Unit Owner, Condominium Manager, Company Administrator, Condominium Staff")]
        //public async Task<IActionResult> Create(CreateConversationViewModel model)
        //{
        //    // Get the user who is initiating the conversation.
        //    var initiator = await _userManager.GetUserAsync(User);
        //    if (initiator == null) return Unauthorized();

        //    // The recipient list must be re-populated every time this action is called,
        //    // both for the initial GET and for POSTs that fail validation.
        //    // This prevents the dropdown from being empty if the page needs to be re-displayed with an error.
        //    var recipients = new List<SelectListItem>();
        //    if (User.IsInRole("Unit Owner"))
        //    {
        //        var assignedUnit = await _context.Units.FirstOrDefaultAsync(u => u.OwnerId == initiator.Id);
        //        if (assignedUnit != null)
        //        {
        //            var condominium = await _context.Condominiums.FindAsync(assignedUnit.CondominiumId);
        //            if (condominium != null)
        //            {
        //                // Add the Condo Manager
        //                if (!string.IsNullOrEmpty(condominium.CondominiumManagerId))
        //                {
        //                    var manager = await _userManager.FindByIdAsync(condominium.CondominiumManagerId);
        //                    if (manager != null)
        //                        recipients.Add(new SelectListItem($"{manager.FirstName} {manager.LastName} (Manager)", manager.Id));
        //                }
        //                // Add the Company Admin
        //                var company = await _companyRepository.GetByIdAsync(condominium.CompanyId);
        //                if (company != null && !string.IsNullOrEmpty(company.ApplicationUserId))
        //                {
        //                    var admin = await _userManager.FindByIdAsync(company.ApplicationUserId);
        //                    if (admin != null)
        //                        recipients.Add(new SelectListItem($"{admin.FirstName} {admin.LastName} (Admin)", admin.Id));
        //                }
        //            }
        //        }
        //    }
        //    // We will add logic for other roles (e.g., Manager, Staff) here in the future.
        //    model.Recipients = recipients;

        //    if (ModelState.IsValid)
        //    {
        //        // Find the unit associated with the initiator (for now, we assume it's an owner).
        //        var userId = initiator.Id;               // string
        //        var unit = await _context.Units
        //            .FirstOrDefaultAsync(u => u.OwnerId == userId);

        //        if (unit == null)
        //        {
        //            ModelState.AddModelError("", "Could not find an associated unit for this user.");
        //            // Because we repopulated the list above, we can now safely return the view.
        //            return View(model);
        //        }

        //        // 1. Create the Conversation object.
        //        var conversation = new Conversation
        //        {
        //            Subject = model.Subject,
        //            InitiatorId = initiator.Id,
        //            AssignedToId = model.RecipientId, // Initially assigned to the chosen recipient
        //            UnitId = unit.Id,
        //            Status = MessageStatus.Pending
        //        };
        //        _context.Conversations.Add(conversation);
        //        await _context.SaveChangesAsync(); // Save to get the new ConversationId

        //        // 2. Create the first Message in that conversation.
        //        var message = new Message
        //        {
        //            Content = model.Message,
        //            SenderId = initiator.Id,
        //            ReceiverId = model.RecipientId, // The first message goes to the chosen recipient
        //            ConversationId = conversation.Id,
        //        };
        //        _context.Messages.Add(message);
        //        await _context.SaveChangesAsync();

        //        // TODO: Use SignalR to notify the specific recipient in real-time.

        //        TempData["StatusMessage"] = "Your new conversation has been created successfully.";
        //        return RedirectToAction(nameof(Index));
        //    }

        //    // If the initial model state was invalid (e.g., no recipient was selected),
        //    // the view is returned with the correctly re-populated dropdown list.
        //    return View(model);
        //}

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
        /// Marks an assigned conversation as <see cref="MessageStatus.InProgress"/>.
        /// Only the assigned staff member (or a manager/admin) can do this.
        /// </summary>
        /// <param name="id">Conversation ID.</param>
        /// <returns>A redirect back to the message list.</returns>
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
    }
}