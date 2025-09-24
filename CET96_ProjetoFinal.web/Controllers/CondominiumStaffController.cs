using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using CET96_ProjetoFinal.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CET96_ProjetoFinal.web.Controllers
{
    /// <summary>
    /// Manages CRUD operations for Condominium Staff users. Access is restricted to Condominium Managers.
    /// </summary>
    [Authorize(Roles = "Company Administrator, Condominium Manager")]
    public class CondominiumStaffController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICondominiumRepository _condominiumRepository;
        private readonly IEmailSender _emailSender;
        private readonly ICompanyRepository _companyRepository;

        public CondominiumStaffController(
            IApplicationUserRepository userRepository,
            UserManager<ApplicationUser> userManager,
            ICondominiumRepository condominiumRepository,
            IEmailSender emailSender,
            ICompanyRepository companyRepository)
        {
            _userRepository = userRepository;
            _userManager = userManager;
            _condominiumRepository = condominiumRepository;
            _emailSender = emailSender;
            _companyRepository = companyRepository;
        }

        // GET: CondominiumStaff?condominiumId=5
        /// <summary>
        /// Displays a list of all staff members for a specific condominium.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium.</param>
        /// <returns>A view with the list of staff members.</returns>
        public async Task<IActionResult> Index(int condominiumId)
        {
            var staffList = await _userRepository.GetStaffByCondominiumIdAsync(condominiumId);

            // Fetch the condominium to display its name in the view title
            var condominium = await _condominiumRepository.GetByIdAsync(condominiumId);

            if (condominium == null)
            {
                return NotFound();
            }

            ViewBag.CondominiumId = condominiumId;
            ViewBag.CondominiumName = condominium.Name;
            ViewBag.CompanyId = condominium.CompanyId;

            return View(staffList);
        }

        /// <summary>
        /// Displays a read-only details view for a specific staff member.
        /// Ensures the logged-in user (Admin or Manager) has permission to view this staff.
        /// </summary>
        /// <param name="id">The string ID (GUID) of the staff member (ApplicationUser) to display.</param>
        /// <returns>The Details view populated with the staff member's data.</returns>
        [Authorize(Roles = "Company Administrator, Condominium Manager")]
        public async Task<IActionResult> Details(string id)
        {
            if (id == null) return NotFound();

            var staffMember = await _userRepository.GetUserByIdAsync(id);
            if (staffMember == null || !staffMember.CondominiumId.HasValue || !staffMember.CompanyId.HasValue)
            {
                return NotFound();
            }

            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (loggedInUser == null) return Unauthorized();

            bool isAuthorized = false;

            // Security Check 1: Is user a Manager assigned to this staff's condo?
            if (User.IsInRole("Condominium Manager"))
            {
                var managersCondo = await _condominiumRepository.GetCondominiumByManagerIdAsync(loggedInUser.Id);
                if (managersCondo != null && managersCondo.Id == staffMember.CondominiumId)
                {
                    isAuthorized = true;
                }
            }
            // Security Check 2: Is user a Company Admin who owns this staff's company?
            else if (User.IsInRole("Company Administrator"))
            {
                if (staffMember.CompanyId == loggedInUser.CompanyId)
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                return RedirectToAction("AccessDenied", "Error"); // Redirect to our new custom error page
            }

            // Get Condo and Company names to display in the view
            var condo = await _condominiumRepository.GetByIdAsync(staffMember.CondominiumId.Value);
            var company = await _companyRepository.GetByIdAsync(staffMember.CompanyId.Value);

            ViewBag.CondominiumName = condo?.Name ?? "N/A";
            ViewBag.CompanyName = company?.Name ?? "N/A";

            return View(staffMember); // Pass the staff member (ApplicationUser) to the new view
        }

        //// GET: CondominiumStaff/Create
        ///// <summary>
        ///// Displays the form to create a new staff member.
        ///// </summary>
        ///// <returns>The create staff member view.</returns>
        //public async Task<IActionResult> Create(int condominiumId)
        //{
        //    // Get the condo first to make sure it exists
        //    var condo = await _condominiumRepository.GetByIdAsync(condominiumId);
        //    if (condo == null)
        //    {
        //        return NotFound();
        //    }

        //    var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
        //    var managedCondominium = await _condominiumRepository.GetCondominiumByManagerIdAsync(loggedInUser.Id);

        //    if (managedCondominium == null)
        //    {
        //        TempData["StatusMessage"] = "Error: You must be assigned a condominium to create staff.";
        //        return RedirectToAction("CondominiumManagerDashboard", "Home");
        //    }

        //    var model = new RegisterCondominiumStaffViewModel
        //    {
        //        CondominiumId = managedCondominium.Id
        //    };

        //    return View(model);
        //}



        // GET: CondominiumStaff/Create
        /// <summary>
        /// Displays the form to create a new staff member for a specific condominium.
        /// This action performs a security check to ensure the currently logged-in user
        /// (either a Company Admin or a Condominium Manager) has permission to access the specified condominium.
        /// </summary>
        /// <param name="condominiumId">The ID of the condominium to add staff to.</param>
        /// <returns>The create staff view, or an Access Denied redirect if unauthorized.</returns>
        public async Task<IActionResult> Create(int condominiumId)
        {
            // Get the condo first to make sure it exists
            var condo = await _condominiumRepository.GetByIdAsync(condominiumId);
            if (condo == null)
            {
                return NotFound();
            }

            // --- START: SECURITY CHECK ---
            // Now, check if the logged-in user (Admin OR Manager) has permission for this condo
            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (loggedInUser == null)
            {
                return Unauthorized(); // Should not happen, but a good safeguard
            }

            bool isAuthorized = false;

            if (User.IsInRole("Condominium Manager"))
            {
                // If they are a Manager, their ID must be assigned to THIS specific condo
                if (condo.CondominiumManagerId == loggedInUser.Id)
                {
                    isAuthorized = true;
                }
            }
            else if (User.IsInRole("Company Administrator"))
            {
                // If they are an Admin, this condo's CompanyId must match THEIR CompanyId
                if (condo.CompanyId == loggedInUser.CompanyId)
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                // If they are neither, or they don't own this condo, deny access.
                return RedirectToAction("AccessDenied", "Account");
            }
            // --- END: SECURITY CHECK ---

            // If we are here, the user is authorized. Show the form.
            var model = new RegisterCondominiumStaffViewModel
            {
                CondominiumId = condominiumId
            };

            return View(model);
        }

        /// <summary>
        /// Handles the submission of the new staff member form. This action creates a new ApplicationUser,
        /// assigns them to the 'Condominium Staff' role, and triggers three district email workflows:
        /// 1. A confirmation email is sent to the new staff member.
        /// 2. A notification email is sent to the logged-in Condominium Manager or Company Admin performing the action.
        /// 3. A notification email is sent to the primary Company Administrator for the company.
        /// </summary>
        /// <param name="model">The view model with the new staff member's details.</param>
        /// <returns>A redirect to the staff list on success, with a status message.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RegisterCondominiumStaffViewModel model)
        {
            // Get the user performing this action (can be an Admin or Manager)
            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (loggedInUser == null)
            {
                return Unauthorized();
            }

            // --- START: NEW SECURITY CHECK (CRITICAL) ---
            // Get the condominium they are trying to add staff TO
            var condo = await _condominiumRepository.GetByIdAsync(model.CondominiumId);
            if (condo == null)
            {
                return NotFound();
            }

            bool isAuthorized = false;
            if (User.IsInRole("Condominium Manager") && condo.CondominiumManagerId == loggedInUser.Id)
            {
                isAuthorized = true; // This manager is assigned to this condo
            }
            else if (User.IsInRole("Company Administrator") && condo.CompanyId == loggedInUser.CompanyId)
            {
                isAuthorized = true; // This admin owns the company this condo belongs to
            }

            if (!isAuthorized)
            {
                // This user is trying to POST data to a condo they do not own. Deny it.
                return RedirectToAction("AccessDenied", "Account");
            }
            // --- END: NEW SECURITY CHECK ---

            if (ModelState.IsValid)
            {
                var userExists = await _userRepository.GetUserByEmailasync(model.Username);
                if (userExists != null)
                {
                    ModelState.AddModelError("Username", "This email is already in use.");
                }

                if (ModelState.IsValid)
                {
                    var user = new ApplicationUser
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        UserName = model.Username,
                        Email = model.Username,
                        PhoneNumber = model.PhoneNumber,
                        DocumentType = model.DocumentType,
                        IdentificationDocument = model.IdentificationDocument,
                        CondominiumId = model.CondominiumId,
                        Profession = model.Profession,
                        CreatedAt = DateTime.UtcNow,
                        UserCreatedId = loggedInUser.Id // Log who created this staff
                    };

                    var result = await _userRepository.AddUserAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        await _userRepository.AddUserToRoleAsync(user, "Condominium Staff");

                        // --- START: ALL EMAIL LOGIC ---

                        // 1. Send CONFIRMATION Email to the NEW STAFF member
                        var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);
                        await _emailSender.SendEmailAsync(user.Email,
                            "Confirm your new CondoManagerPrime Account",
                            $"<p>An account has been created for you. Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a></p>");

                        // 2. Send NOTIFICATION Email to the ADMIN/MANAGER (who is logged in)
                        await _emailSender.SendEmailAsync(loggedInUser.Email,
                            $"New Staff Created: {user.FirstName} {user.LastName}",
                            $"<p>You have successfully created a new staff account for {user.FirstName} {user.LastName} ({user.Email}) for the condominium {condo.Name}.</p>");

                        // 3. Send NOTIFICATION Email to the main COMPANY ADMINISTRATOR
                        // (Only if the person doing this isn't already the main admin)
                        var company = await _companyRepository.GetByIdAsync(condo.CompanyId);
                        var companyAdmin = await _userRepository.GetUserByIdAsync(company.ApplicationUserId);

                        if (companyAdmin != null && companyAdmin.Id != loggedInUser.Id)
                        {
                            await _emailSender.SendEmailAsync(companyAdmin.Email,
                                $"New Staff Added to Company: {user.FirstName} {user.LastName}",
                                $"<p>This is a notification that your user ({loggedInUser.FirstName} {loggedInUser.LastName}) has created a new staff account:</p>" +
                                $"<ul><li><b>Staff:</b> {user.FirstName} {user.LastName} ({user.Email})</li><li><b>Condominium:</b> {condo.Name}</li><li><b>Company:</b> {company.Name}</li></ul>");
                        }
                        // --- END: ALL EMAIL LOGIC ---

                        TempData["StatusMessage"] = $"Condominium staff member '{user.FirstName} {user.LastName}' created successfully. A confirmation email has been sent to their address.";
                        return RedirectToAction(nameof(Index), new { condominiumId = user.CondominiumId });
                    }

                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            return View(model);
        }

        //// POST: CondominiumStaff/Create
        ///// <summary>
        ///// Handles the submission of the new staff member form. This action creates a new ApplicationUser,
        ///// assigns them to the 'Condominium Staff' role, and triggers three distinct email workflows:
        ///// 1. A confirmation email is sent to the new staff member's email address.
        ///// 2. A notification email is sent to the logged-in Condominium Manager who performed the action.
        ///// 3. A notification email is sent to the primary Company Administrator who owns the parent company.
        ///// </summary>
        ///// <param name="model">The view model with the new staff member's details.</param>
        ///// <returns>A redirect to the staff list on success, with a status message.</returns>        [HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(RegisterCondominiumStaffViewModel model)
        //{
        //    // Get the currently logged-in Condominium Manager
        //    var condoManager = await _userRepository.GetUserByEmailasync(User.Identity.Name);
        //    if (condoManager == null)
        //    {
        //        // This should not happen if they are authorized, but it's a safe check.
        //        return Unauthorized();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        var userExists = await _userRepository.GetUserByEmailasync(model.Username);
        //        if (userExists != null)
        //        {
        //            ModelState.AddModelError("Username", "This email is already in use.");
        //        }

        //        if (ModelState.IsValid)
        //        {
        //            var user = new ApplicationUser
        //            {
        //                FirstName = model.FirstName,
        //                LastName = model.LastName,
        //                UserName = model.Username,
        //                Email = model.Username,
        //                PhoneNumber = model.PhoneNumber,
        //                DocumentType = model.DocumentType,
        //                IdentificationDocument = model.IdentificationDocument,
        //                CondominiumId = model.CondominiumId,
        //                Profession = model.Profession,
        //                CreatedAt = DateTime.UtcNow
        //            };

        //            var result = await _userRepository.AddUserAsync(user, model.Password);

        //            if (result.Succeeded)
        //            {
        //                await _userRepository.AddUserToRoleAsync(user, "Condominium Staff");
        //                // --- START: ALL EMAIL LOGIC ---

        //                // 1. Send CONFIRMATION Email to the NEW STAFF member
        //                var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
        //                var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);
        //                await _emailSender.SendEmailAsync(user.Email,
        //                    "Confirm your new CondoManagerPrime Account",
        //                    $"<p>An account has been created for you. Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a></p>");

        //                // 2. (New Feature) Send NOTIFICATION Email to the CONDO MANAGER (who is logged in)
        //                var condo = await _condominiumRepository.GetByIdAsync(model.CondominiumId);
        //                await _emailSender.SendEmailAsync(condoManager.Email,
        //                    $"New Staff Created: {user.FirstName} {user.LastName}",
        //                    $"<p>You have successfully created a new staff account for {user.FirstName} {user.LastName} ({user.Email}) for the condominium {condo?.Name}.</p>");

        //                // 3. (New Feature) Send NOTIFICATION Email to the main COMPANY ADMINISTRATOR
        //                if (condo != null)
        //                {
        //                    var company = await _companyRepository.GetByIdAsync(condo.CompanyId);
        //                    // company.ApplicationUserId holds the ID of the main admin
        //                    var companyAdmin = await _userRepository.GetUserByIdAsync(company.ApplicationUserId);

        //                    if (companyAdmin != null)
        //                    {
        //                        await _emailSender.SendEmailAsync(companyAdmin.Email,
        //                            $"New Staff Added to Company: {user.FirstName} {user.LastName}",
        //                            $"<p>This is a notification that your Condominium Manager ({condoManager.FirstName} {condoManager.LastName}) has created a new staff account:</p>" +
        //                            $"<ul><li><b>Staff:</b> {user.FirstName} {user.LastName} ({user.Email})</li><li><b>Condominium:</b> {condo.Name}</li><li><b>Company:</b> {company.Name}</li></ul>");
        //                    }
        //                }
        //                // --- END: ALL EMAIL LOGIC ---
        //                TempData["StatusMessage"] = $"Condominium staff member '{user.FirstName} {user.LastName}' created successfully.";
        //                return RedirectToAction(nameof(Index), new { condominiumId = user.CondominiumId });
        //            }

        //            foreach (var error in result.Errors)
        //            {
        //                ModelState.AddModelError(string.Empty, error.Description);
        //            }
        //        }
        //    }
        //    return View(model);
        //}

        // GET: CondominiumStaff/Edit/5
        /// <summary>
        /// Displays the form to edit an existing staff member's details.
        /// </summary>
        /// <param name="id">The ID of the staff member to edit.</param>
        /// <returns>The edit staff member view.</returns>
        public async Task<IActionResult> Edit(string id)
        {
            var staffMember = await _userRepository.GetUserByIdAsync(id);
            if (staffMember == null)
            {
                return NotFound();
            }

            // --- SECURITY CHECK ---
            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            bool isAuthorized = false;

            if (User.IsInRole("Company Administrator") && staffMember.CompanyId == loggedInUser.CompanyId)
            {
                // Admins can edit any staff in their own company.
                isAuthorized = true;
            }
            else if (User.IsInRole("Condominium Manager"))
            {
                // Managers can only edit staff in their assigned condominium.
                var managedCondominium = await _condominiumRepository.GetCondominiumByManagerIdAsync(loggedInUser.Id);
                if (managedCondominium != null && staffMember.CondominiumId == managedCondominium.Id)
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                TempData["StatusMessage"] = "Error: You do not have permission to edit this staff member.";
                return RedirectToAction("Index", "CondominiumManager");
            }
            // --- END SECURITY CHECK ---

            var model = new EditAccountViewModel
            {
                Id = staffMember.Id,
                FirstName = staffMember.FirstName,
                LastName = staffMember.LastName,
                PhoneNumber = staffMember.PhoneNumber
            };

            return View(model);
        }


        // POST: CondominiumStaff/Edit/5
        /// <summary>
        /// Handles the submission of the edit staff member form.
        /// </summary>
        /// <param name="model">The view model with the updated staff member's details.</param>
        /// <returns>A redirect to the staff list on success.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditAccountViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // If validation fails, return to the form with errors.
                return View(model);
            }

            var staffMember = await _userRepository.GetUserByIdAsync(model.Id);
            if (staffMember == null)
            {
                return NotFound();
            }

            // --- SECURITY CHECK (again) ---
            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            bool isAuthorized = false;
            if (User.IsInRole("Company Administrator") && staffMember.CompanyId == loggedInUser.CompanyId)
            {
                isAuthorized = true;
            }
            else if (User.IsInRole("Condominium Manager"))
            {
                var managedCondominium = await _condominiumRepository.GetCondominiumByManagerIdAsync(loggedInUser.Id);
                if (managedCondominium != null && staffMember.CondominiumId == managedCondominium.Id)
                {
                    isAuthorized = true;
                }
            }

            if (!isAuthorized)
            {
                TempData["StatusMessage"] = "Error: You do not have permission to perform this action.";
                return RedirectToAction("Index", "CondominiumManager");
            }
            // --- END SECURITY CHECK ---

            // Update the user properties from the ViewModel.
            staffMember.FirstName = model.FirstName;
            staffMember.LastName = model.LastName;
            staffMember.PhoneNumber = model.PhoneNumber;
            staffMember.UpdatedAt = DateTime.UtcNow;
            staffMember.UserUpdatedId = loggedInUser.Id;

            var result = await _userManager.UpdateAsync(staffMember);

            if (result.Succeeded)
            {
                TempData["StatusMessage"] = "Staff member updated successfully.";

                // --- CORRECT REDIRECT LOGIC ---
                // Send the user back to the dashboard they came from.
                if (User.IsInRole("Condominium Manager"))
                {
                    return RedirectToAction("Index", "CondominiumManager");
                }

                // Fallback for Company Admin
                return RedirectToAction("AllUsersByCompany", "Account", new { id = staffMember.CompanyId });
            }

            // If update fails, display errors.
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // POST: CondominiumStaff/Deactivate/5
        /// <summary>
        /// Deactivates a staff member's account.
        /// </summary>
        /// <param name="id">The ID of the staff member to deactivate.</param>
        /// <returns>A redirect to the staff list.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deactivate(string id)
        {
            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            var managedCondominium = await _condominiumRepository.GetCondominiumByManagerIdAsync(loggedInUser.Id);

            if (managedCondominium == null)
            {
                TempData["StatusMessage"] = "Error: You are not assigned to a condominium.";
                return RedirectToAction("CondominiumManagerDashboard", "Home");
            }

            var staffMember = await _userRepository.GetUserByIdAsync(id);
            if (staffMember == null)
            {
                return NotFound();
            }

            // CRUCIAL SECURITY CHECK: Ensure the staff member belongs to the logged-in manager's condominium.
            if (staffMember.CondominiumId != managedCondominium.Id)
            {
                TempData["StatusMessage"] = "Error: You do not have permission to perform this action.";
                return RedirectToAction("CondominiumManagerDashboard", "Home");
            }

            // Deactivate the user
            staffMember.DeactivatedAt = DateTime.UtcNow;
            staffMember.DeactivatedByUserId = _userManager.GetUserId(User);
            staffMember.UpdatedAt = DateTime.UtcNow;
            staffMember.UserUpdatedId = _userManager.GetUserId(User);

            var result = await _userManager.UpdateAsync(staffMember);

            if (result.Succeeded)
            {
                // Set a lockout end date to prevent login
                await _userManager.SetLockoutEndDateAsync(staffMember, DateTimeOffset.MaxValue);
                TempData["StatusMessage"] = "Staff member deactivated successfully.";
            }
            else
            {
                TempData["StatusMessage"] = "Error deactivating staff member.";
            }

            return RedirectToAction("CondominiumManagerDashboard", "Home");
        }

        // POST: CondominiumStaff/Activate/5
        /// <summary>
        /// Activates a staff member's account.
        /// </summary>
        /// <param name="id">The ID of the staff member to activate.</param>
        /// <returns>A redirect to the staff list.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Activate(string id)
        {
            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            var managedCondominium = await _condominiumRepository.GetCondominiumByManagerIdAsync(loggedInUser.Id);

            if (managedCondominium == null)
            {
                TempData["StatusMessage"] = "Error: You are not assigned to a condominium.";
                return RedirectToAction("CondominiumManagerDashboard", "Home");
            }

            var staffMember = await _userRepository.GetUserByIdAsync(id);
            if (staffMember == null)
            {
                return NotFound();
            }

            // CRUCIAL SECURITY CHECK: Ensure the staff member belongs to the logged-in manager's condominium.
            if (staffMember.CondominiumId != managedCondominium.Id)
            {
                TempData["StatusMessage"] = "Error: You do not have permission to perform this action.";
                return RedirectToAction("CondominiumManagerDashboard", "Home");
            }

            // Activate the user
            staffMember.DeactivatedAt = null;
            staffMember.DeactivatedByUserId = null;
            staffMember.UpdatedAt = DateTime.UtcNow;
            staffMember.UserUpdatedId = _userManager.GetUserId(User);

            var result = await _userManager.UpdateAsync(staffMember);

            if (result.Succeeded)
            {
                // Remove any lockout end date
                await _userManager.SetLockoutEndDateAsync(staffMember, null);
                TempData["StatusMessage"] = "Staff member activated successfully.";
            }
            else
            {
                TempData["StatusMessage"] = "Error activating staff member.";
            }

            return RedirectToAction("CondominiumManagerDashboard", "Home");
        }
    }
}