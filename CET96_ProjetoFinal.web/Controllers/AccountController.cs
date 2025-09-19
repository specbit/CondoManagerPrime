using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Repositories;
using CET96_ProjetoFinal.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;

namespace CET96_ProjetoFinal.web.Controllers
{
    /// <summary>
    /// Manages all user account-related actions such as registration, login, logout,
    /// and email confirmation.
    /// </summary>
    public class AccountController : Controller
    {
        private readonly IApplicationUserRepository _userRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICondominiumRepository _condominiumRepository;

        public AccountController(
            IApplicationUserRepository userRepository,
            ICompanyRepository companyRepository,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager,
            ICondominiumRepository condominiumRepository)
        {
            _userRepository = userRepository;
            _companyRepository = companyRepository;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _userManager = userManager;
            _condominiumRepository = condominiumRepository;
        }

        // GET: /Account/Login
        /// <summary>
        /// Displays the user login page.
        /// </summary>
        /// <returns>The login view.</returns>

        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
            {
                // If the user is already logged in, send them to the home page.
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        /// <summary>
        /// Handles the submission of the login form.
        /// </summary>
        /// <param name="model">The view model containing the user's login credentials.</param>
        /// <returns>A redirect to the home page on successful login, or the login view with errors on failure.</returns>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var result = await _userRepository.LoginAsync(model);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty,
                    result.IsNotAllowed ? "You must confirm your email before you can log in."
                                        : "Invalid login attempt.");
                return View(model);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        /// <summary>
        /// Displays the user registration page.
        /// </summary>
        /// <returns>The registration view.</returns>
        [AllowAnonymous]
        public IActionResult RegisterCompanyAndAdmin()
        {
            return View();
        }

        // POST: /Account/Register
        /// <summary>
        /// Handles the HTTP POST request for new user registration. It validates the submitted data,
        /// creates a new ApplicationUser with the 'Company Administrator' role, and sends a
        /// confirmation email.
        /// </summary>
        /// <param name="model">The registration view model containing the user's and company's initial information.</param>
        /// <returns>
        /// The RegistrationConfirmation view on success.
        /// Returns the Register view with validation errors if the model is invalid or the email is already in use.
        /// </returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterCompanyAndAdmin(RegisterCompanyAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetUserByEmailasync(model.Username);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        UserName = model.Username,
                        Email = model.Username,
                        IdentificationDocument = model.IdentificationDocument,
                        DocumentType = model.DocumentType,
                        PhoneNumber = model.PhoneNumber,
                        CompanyName = model.CompanyName
                    };

                    var result = await _userRepository.AddUserAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        // Ensure the "Company Administrator" role exists before adding the user to it.
                        await _userRepository.AddUserToRoleAsync(user, "Company Administrator"); // Add the user to the "Company Administrator" role

                        // --- EMAIL CONFIRMATION LOGIC ---
                        var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationLink = Url.Action("ConfirmEmail", "Account",
                            new { userId = user.Id, token }, Request.Scheme);

                        await _emailSender.SendEmailAsync(model.Username,
                            "Confirm your email for CondoManagerPrime",
                            $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a>");
                        // --- END EMAIL CONFIRMATION LOGIC ---

                        var modelForView = new RegistrationConfirmationViewModel
                        {
                            ConfirmationLink = confirmationLink
                        };

                        // Show a page telling the user to check their email. NO company is created here.
                        return View("RegistrationConfirmation", modelForView);
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    ModelState.AddModelError("Username", "An account with this email already exists. Please log in instead.");
                }
            }
            return View(model);
        }

        [Authorize(Roles = "Company Administrator")]
        public IActionResult RegisterNewCondominiumManager(int companyId)
        {
            var model = new RegisterCondominiumManagerViewModel
            {
                CompanyId = companyId
            };
            // Pass the CompanyId to the registration form
            return View("RegisterNewCondominiumManager", model);
        }

        // POST: /Account/Register
        [ValidateAntiForgeryToken]
        [HttpPost]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> RegisterNewCondominiumManager(RegisterCondominiumManagerViewModel model)
        {
            var loggedUser = User.Identity.Name;
            var loggedInUser = await _userRepository.GetUserByEmailasync(loggedUser);

            if (loggedInUser == null) return RedirectToAction("Index", "Home");

            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetUserByEmailasync(model.Username);

                if (user == null)
                {
                    user = new ApplicationUser
                    {
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        UserName = model.Username,
                        Email = model.Username,
                        IdentificationDocument = model.IdentificationDocument,
                        DocumentType = model.DocumentType,
                        PhoneNumber = model.PhoneNumber,
                        UserCreatedId = loggedInUser.Id,
                        CompanyId = model.CompanyId
                    };

                    var result = await _userRepository.AddUserAsync(user, model.Password);

                    if (result.Succeeded)
                    {
                        await _userRepository.AddUserToRoleAsync(user, "Condominium Manager"); // Add the user to the "Condominium Manager" role

                        // --- EMAIL CONFIRMATION LOGIC ---
                        var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationLink = Url.Action("ConfirmEmail", "Account",
                            new { userId = user.Id, token }, Request.Scheme);

                        await _emailSender.SendEmailAsync(model.Username,
                            "Confirm your email for CondoManagerPrime",
                            $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a>");
                        // --- END EMAIL CONFIRMATION LOGIC ---

                        var modelForView = new RegistrationConfirmationViewModel
                        {
                            ConfirmationLink = confirmationLink
                        };

                        // Show a page telling the user to check their email. 
                        return RedirectToAction("AllUsersByCompany", "Account", new { id = model.CompanyId });
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "This email is already in use.");
                }
            }
            return View(model);
        }

        // POST: /Account/Logout
        /// <summary>
        /// Handles the user logout process.
        /// </summary>
        /// <returns>A redirect to the home page.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _userRepository.LogoutAsync();
            // After logging out, send the user to the home page.
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ChangePassword
        /// <summary>
        /// Displays the change password page for the current user.
        /// </summary>
        /// <returns>The change password view.</returns>
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
        /// <summary>
        /// Handles the submission of the change password form.
        /// </summary>
        /// <param name="model">The view model containing the old and new password information.</param>
        /// <returns>A redirect to the change password page on success, or the view with errors on failure.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            // First, check if the new password is the same as the old one.
            if (model.OldPassword == model.NewPassword)
            {
                ModelState.AddModelError("NewPassword", "The new password cannot be the same as the current password.");
            }

            if (!ModelState.IsValid)
            {
                // Clear the password fields before sending the model back to the view for security.
                model.OldPassword = string.Empty;
                model.NewPassword = string.Empty;
                model.ConfirmPassword = string.Empty;

                return View(model);
            }

            var user = await _userRepository.GetUserByEmailasync(User.Identity.Name);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            var result = await _userRepository.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            // After a successful password change, we must refresh the user's
            // login cookie to update it with the new security stamp.
            await _signInManager.RefreshSignInAsync(user);

            TempData["StatusMessage"] = "Your password has been changed successfully.";

            return RedirectToAction("ChangePassword");
        }

        ///// <summary>
        ///// Handles the link clicked by a user from their email to confirm their account.
        ///// It validates the user and token, confirms the email, signs the user in, and
        ///// redirects them to the company creation flow.
        ///// </summary>
        ///// <param name="userId">The ID of the user to confirm.</param>
        ///// <param name="token">The confirmation token.</param>
        ///// <returns>A redirect to the company creation page on success, or an er
        //[HttpGet]
        //[AllowAnonymous]
        //public async Task<IActionResult> ConfirmEmail(string userId, string token)
        //{
        //    if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        //    {
        //        return RedirectToAction("Index", "Home");
        //    }

        //    var user = await _userRepository.GetUserByIdAsync(userId);
        //    if (user == null)
        //    {
        //        return View("Error");
        //    }

        //    var result = await _userRepository.ConfirmEmailAsync(user, token);

        //    if (result.Succeeded)
        //    {
        //        // 1. Sign the user in to create a session.
        //        await _signInManager.SignInAsync(user, isPersistent: false);

        //        // 2. CRUCIAL STEP: Refresh the sign-in session. This forces the user's
        //        // roles and claims to be reloaded into the cookie immediately.
        //        await _signInManager.RefreshSignInAsync(user);

        //        // 3. Now that the session is guaranteed to be valid and have the correct roles,
        //        // check if they have a company.
        //        var companyExists = await _companyRepository.DoesCompanyExistForUserAsync(user.Id);

        //        if (!companyExists)
        //        {
        //            // The redirect will now succeed because the user is properly authenticated WITH their roles.
        //            //return RedirectToAction("Create", "Companies");
        //            return RedirectToAction("Create", "Companies", new { companyName = user.CompanyName });
        //        }

        //        // If for some reason they already have a company, send them home.
        //        return RedirectToAction("Index", "Home");
        //    }

        //    // If confirmation fails, show an error.
        //    return View("Error");
        //}

        /// <summary>
        /// Handles the email confirmation link clicked by a user, regardless of their role.
        /// </summary>
        /// <remarks>
        /// This action performs common validation and the core email confirmation. It then dispatches 
        /// to role-specific private helper methods to handle the unique follow-up logic for each role.
        /// </remarks>
        /// <param name="userId">The ID of the user to confirm.</param>
        /// <param name="token">The confirmation token from the email link.</param>
        /// <returns>The appropriate action result based on the user's role and confirmation success.</returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            // --- Step 1: Common Logic (Handles validation for ALL roles) ---
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                // Pass a valid ErrorViewModel to the Error view
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            var result = await _userRepository.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                // Also pass a valid ErrorViewModel here
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            // --- Step 2: Dispatch to the correct helper based on the user's role ---
            if (await _userManager.IsInRoleAsync(user, "Company Administrator"))
            {
                return await HandleCompanyAdminConfirmation(user);
            }
            else if (await _userManager.IsInRoleAsync(user, "Condominium Manager") || await _userManager.IsInRoleAsync(user, "Condominium Staff"))
            {
                return await HandleStaffOrManagerConfirmation(user);
            }

            // Default fallback for other potential roles (like Unit Owner in the future)
            return View("ConfirmationSuccess");
        }

        // --- Private Helper Methods for ConfirmEmail => Dispatchers ---

        /// <summary>
        /// Handles the specific follow-up actions for a newly confirmed Company Administrator.
        /// </summary>
        /// <param name="user">The confirmed Company Administrator user.</param>
        /// <returns>A redirect to the company creation page or the main dashboard.</returns>
        private async Task<IActionResult> HandleCompanyAdminConfirmation(ApplicationUser user)
        {
            // 1. Log the new admin in automatically.
            await _signInManager.SignInAsync(user, isPersistent: false);
            await _signInManager.RefreshSignInAsync(user);

            // 2. Check if they need to create their company.
            var companyExists = await _companyRepository.DoesCompanyExistForUserAsync(user.Id);
            if (!companyExists)
            {
                // 3. Send them to the "Create Company" form, passing their pre-filled company name.
                return RedirectToAction("Create", "Companies", new { companyName = user.CompanyName });
            }

            // If they already have a company, send them home.
            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Handles the specific follow-up actions for a newly confirmed Manager or Staff member.
        /// It notifies the creator AND (if different) the assigned Condo Manager, then sends the user to the Login page.
        /// </summary>
        /// <param name="user">The confirmed Condominium Manager or Staff user.</param>
        /// <returns>A redirect to the Login page with a success message.</returns>
        private async Task<IActionResult> HandleStaffOrManagerConfirmation(ApplicationUser user)
        {
            ApplicationUser creator = null; // We'll store the creator to prevent duplicate emails

            // 1. Notify the Creator (This can be an Admin or a Manager)
            if (!string.IsNullOrEmpty(user.UserCreatedId))
            {
                creator = await _userRepository.GetUserByIdAsync(user.UserCreatedId);
                if (creator != null)
                {
                    await _emailSender.SendEmailAsync(
                        creator.Email,
                        $"Account Confirmed: {user.FirstName} {user.LastName}",
                        $"This is a notification that the user {user.FirstName} {user.LastName} ({user.Email}) has successfully confirmed their account and can now log in."
                    );
                }
            }

            // 2. --- Notify Assigned Condo Manager (IF they are a different person) ---
            bool isStaff = await _userManager.IsInRoleAsync(user, "Condominium Staff");

            if (isStaff && user.CondominiumId.HasValue)
            {
                var condo = await _condominiumRepository.GetByIdAsync(user.CondominiumId.Value);

                // Check if the condo exists AND has a manager assigned
                if (condo != null && !string.IsNullOrEmpty(condo.CondominiumManagerId))
                {
                    // --- THIS IS THE DUPLICATE PREVENTION CHECK ---
                    // Only send this email if the Condo Manager is NOT the same person who created the account
                    if (creator == null || creator.Id != condo.CondominiumManagerId)
                    {
                        var condoManager = await _userRepository.GetUserByIdAsync(condo.CondominiumManagerId);
                        if (condoManager != null)
                        {
                            await _emailSender.SendEmailAsync(
                                condoManager.Email,
                                $"New Staff Account Confirmed: {user.FirstName} {user.LastName}",
                                $"This is a notification that your new staff member {user.FirstName} {user.LastName} ({user.Email}) " +
                                $"for condominium '{condo.Name}' has successfully confirmed their account and is now ready to log in."
                            );
                        }
                    }
                }
            }

            // --- START: EXPLANATION COMMENT ---
            // By not logging in the new user (e.g., await _signInManager.SignInAsync(...)),
            // we prevent their new login cookie from overwriting the session of the
            // Administrator or Manager who is currently logged in and testing the application.
            //
            // Instead, we confirm their account, send notifications, and just return
            // a static success view. This has no effect on any other login session.
            // --- END: EXPLANATION COMMENT ---

            // 3. Show a generic success page.
            return View("ConfirmationSuccess");
        }

        /// <summary>
        /// Displays the main account management page for the logged-in user.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> AccountDetails()
        {
            var user = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (user == null) return NotFound();

            return View(user);
        }

        //// GET: All Users By Company Administrator
        //[Authorize(Roles = "Company Administrator")]
        //public async Task<IActionResult> AllUsersByCompany(int id, bool showInactive = false)
        //{
        //    int companyId = id;

        //    var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
        //    if (loggedInUser == null) return RedirectToAction("Index", "Home");

        //    var company = await _companyRepository.GetByIdAsync(companyId);

        //    if (company == null)
        //    {
        //        return NotFound();
        //    }

        //    ViewBag.CompanyName = company?.Name; // Pass the company doesn't thow an exception if null
        //    ViewBag.CompanyId = company.Id; // Pass the company ID to the view

        //    // We declare the list here and populate it based on the showInactive flag.
        //    IEnumerable<ApplicationUser> users;
        //    if (showInactive)
        //    {
        //        users = await _userRepository.GetInactiveUsersByCompanyIdAsync(companyId);
        //        ViewBag.Title = "Inactive Condominium Managers";
        //        ViewBag.ShowingInactive = true;
        //    }
        //    else
        //    {
        //        users = await _userRepository.GetActiveUsersByCompanyIdAsync(companyId);
        //        ViewBag.Title = "Condominium Managers";
        //        ViewBag.ShowingInactive = false;
        //    }

        //    var userViewModelList = new List<ApplicationUserViewModel>();

        //    foreach (var user in users)
        //    {
        //        var roles = await _userRepository.GetUserRolesAsync(user);
        //        string? assignedCondoName = null;

        //        // Check ONLY for users who are in the "Condominium Manager" role
        //        if (roles.Contains("Condominium Manager"))
        //        {
        //            var assignment = await _condominiumRepository.GetCondominiumByManagerIdAsync(user.Id);
        //            // If an assignment is found, get its name
        //            assignedCondoName = assignment?.Name;

        //            userViewModelList.Add(new ApplicationUserViewModel
        //            {
        //                Id = user.Id,
        //                FirstName = user.FirstName,
        //                LastName = user.LastName,
        //                UserName = user.UserName,
        //                IsDeactivated = user.DeactivatedAt.HasValue,
        //                Roles = roles,
        //                AssignedCondominiumName = assignedCondoName
        //            });
        //        }
        //    }

        //    var model = new ManagersListViewModel
        //    {
        //        AllUsers = userViewModelList
        //    };

        //    return View(model);
        //}

        /// <summary>
        /// Company admin hub: shows Condominium Managers (with active/inactive toggle)
        /// and the list of **active** Condominium Staff, on the same page.
        /// </summary>
        /// <param name="id">Company ID.</param>
        /// <param name="showInactive">
        /// If true, the Managers table shows deactivated managers; the Staff table remains active-only.
        /// </param>
        /// <returns>View with two tables: Managers and Staff.</returns>
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> AllUsersByCompany(int id, bool showInactive = false)
        {
            // Login guard
            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (loggedInUser == null) return RedirectToAction("Index", "Home");

            // Load company & set ViewBags 
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null) return NotFound();

            // Page context
            ViewBag.CompanyName = company.Name;
            ViewBag.CompanyId = company.Id;
            ViewBag.Title = "Condominium Managers and Staff";
            ViewBag.ShowingInactiveManagers = showInactive;

            // Delegate building/filtering/mapping to the helper (which calls GetUserRolesAsync inside).

            // Managers list respects the 'showInactive' toggle 
            var managers = await BuildUserListByRoleAsync(id, "Condominium Manager", showInactive);

            // Staff table is always ACTIVE-ONLY on this combined page (clear, predictable)
            // (If I want a toggle later, easy to add a 'showInactiveStaff' param.)
            var staff = await BuildUserListByRoleAsync(id, "Condominium Staff", showInactive: false);

            var model = new CompanyUsersViewModel
            {
                CompanyId = company.Id,
                CompanyName = company.Name,
                Managers = managers,
                Staff = staff
            };

            return View(model);
        }

        /// <summary>
        /// Displays the page for a user to edit their own account details.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> EditAccount(string id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var model = new EditAccountViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                PhoneNumber = user.PhoneNumber,
                CompanyId = user.CompanyId ?? 0 // Pass the CompanyId to the view
            };

            return View(model);
        }

        /// <summary>
        /// Handles the submission of the edit account form.
        /// </summary>
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAccount(EditAccountViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _userRepository.GetUserByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                // Update the user's properties
                user.FirstName = model.FirstName;
                user.LastName = model.LastName;
                user.PhoneNumber = model.PhoneNumber;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["StatusMessage"] = "Your profile has been updated successfully.";

                    // Redirect back to the correct company's user list
                    var companyId = user.CompanyId ?? 0;
                    return RedirectToAction("AllUsersByCompany", "Account", new { id = companyId });
                }


                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        // GET: Link Condominuim Manager to Condominium
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> LinkManagerToCondominium(string id)
        {
            var condominiumManager = await _userRepository.GetUserByIdAsync(id);

            if (condominiumManager == null)
            {
                TempData["StatusMessage"] = "Error: Condominium Manager not found.";

                return NotFound();
            }

            // Check if the manager is already assigned to a condominium
            var currentAssignment = await _condominiumRepository.GetCondominiumByManagerIdAsync(id);

            var loggedUser = User.Identity.Name;
            var loggedInUser = await _userRepository.GetUserByEmailasync(loggedUser);

            // Fetch the list of condominiums for the select list
            var condominiums = await _condominiumRepository.GetUnassignedCondominiumsByCompanyAdminAsync(loggedInUser.Id);

            // Get the companyId from the manager
            var companyId = condominiumManager.CompanyId;

            var model = new LinkManagerToCondominiumViewModel
            {
                UserId = condominiumManager.Id,
                FullName = $"{condominiumManager.FirstName} {condominiumManager.LastName}",
                CondominiumsList = condominiums,

                // If an assignment was found, store its name in the ViewModel.
                CurrentlyAssignedCondominiumName = currentAssignment?.Name,

                // Set the CompanyId for the view
                CompanyId = companyId.GetValueOrDefault()
            };

            return View(model);
        }

        // This action handles the form submission for linking a manager.
        /// <summary>
        /// Handles the form submission for linking a Condominium Manager to a specific Condominium.
        /// </summary>
        /// <remarks>
        /// This action validates the input, updates the condominium with the new manager ID,
        /// ensures the manager's CompanyId is consistent, sends notifications to the manager, 
        /// the administrator, and the company's official email, and redirects the administrator 
        /// back to the user list.
        /// </remarks>
        /// <param name="model">The view model containing the user ID and the selected condominium ID.</param>
        /// <returns>A redirect to the user list for the company.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> LinkManagerToCondominium(LinkManagerToCondominiumViewModel model)
        {
            // 1. Validate the input.
            var condominiumManager = await _userRepository.GetUserByIdAsync(model.UserId);
            if (condominiumManager == null)
            {
                return NotFound();
            }

            // Check if the user is deactivated before allowing the assignment.
            if (condominiumManager.DeactivatedAt.HasValue)
            {
                TempData["StatusMessage"] = $"Error: Cannot assign a condominium to a deactivated user. Please activate the account first.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = condominiumManager.CompanyId });
            }

            if (model.SelectedCondominiumId == null || model.SelectedCondominiumId == 0)
            {
                // If no condominium was selected, add an error and return to the form.
                ModelState.AddModelError(string.Empty, "You must select a condominium.");
                // Reload the necessary data before returning to the view.
                var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
                model.CondominiumsList = await _condominiumRepository.GetUnassignedCondominiumsByCompanyAdminAsync(loggedInUser.Id);
                model.FullName = $"{condominiumManager.FirstName} {condominiumManager.LastName}";
                model.CompanyId = condominiumManager.CompanyId ?? 0;
                var existing = await _condominiumRepository.GetCondominiumByManagerIdAsync(model.UserId);
                model.CurrentlyAssignedCondominiumName = existing?.Name;
                return View(model);
            }

            // 2. Find the manager's CURRENT assignment.
            var currentAssignment = await _condominiumRepository.GetCondominiumByManagerIdAsync(model.UserId);

            // 3. If they are currently assigned somewhere else, un-assign them.
            if (currentAssignment != null && currentAssignment.Id != model.SelectedCondominiumId.Value)
            {
                currentAssignment.CondominiumManagerId = null;
                _condominiumRepository.Update(currentAssignment);
            }

            // 4. Fetch the condominium to be updated from the database.
            var condominiumToUpdate = await _condominiumRepository.GetByIdAsync(model.SelectedCondominiumId.Value);
            if (condominiumToUpdate == null)
            {
                return NotFound();
            }

            // 5. Assign the manager's ID to the condominium.
            condominiumToUpdate.CondominiumManagerId = model.UserId;

            // 6. Save the condominium changes.
            _condominiumRepository.Update(condominiumToUpdate);
            await _condominiumRepository.SaveAllAsync();

            // 7. Ensure the user's CompanyId matches.
            condominiumManager.CompanyId = condominiumToUpdate.CompanyId;
            await _userRepository.UpdateUserAsync(condominiumManager);

            // 8. Send email notification to the Condo Manager
            await _emailSender.SendEmailAsync(
                condominiumManager.Email,
                "New Condominium Assignment",
                $"Hello {condominiumManager.FirstName},<br><br>You have been assigned to manage the condominium: <strong>{condominiumToUpdate.Name}</strong>."
            );

            // 9. Send a confirmation record to the logged-in Company Admin
            var loggedInAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (loggedInAdmin != null)
            {
                await _emailSender.SendEmailAsync(
                    loggedInAdmin.Email,
                    "Record of Manager Assignment",
                    $"This is a confirmation that you have successfully assigned the manager <strong>{condominiumManager.FirstName} {condominiumManager.LastName}</strong> to the condominium <strong>{condominiumToUpdate.Name}</strong>."
                );
            }

            // 10. Send a notification to the Company's official email address
            var company = await _companyRepository.GetByIdAsync(condominiumToUpdate.CompanyId);
            if (company != null && !string.IsNullOrEmpty(company.Email))
            {
                await _emailSender.SendEmailAsync(
                    company.Email,
                    $"Manager Assigned for {condominiumToUpdate.Name}",
                    $"This is an official notification that the user {condominiumManager.FirstName} {condominiumManager.LastName} has been assigned as the manager for the condominium '{condominiumToUpdate.Name}'."
                );
            }

            TempData["StatusMessage"] = $"Manager has been successfully linked to condominium '{condominiumToUpdate.Name}'.";

            // 11. Redirect back to the list of managers.
            return RedirectToAction("AllUsersByCompany", new { id = condominiumToUpdate.CompanyId });
        }

        /// <summary>
        /// Un-assigns a Condominium Manager from their currently assigned condominium by setting the
        /// Condominium's 'CondominiumManagerId' property to null.
        /// This action also triggers two email notifications upon success:
        /// 1. A notification is sent to the Condominium Manager who was just dismissed.
        /// 2. A confirmation receipt is sent to the Company Administrator who performed the action.
        /// </summary>
        /// <param name="userId">The ID of the manager to dismiss from their condominium assignment.</param>
        /// <returns>A redirect to the company's user list with a status message.</returns>        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> DismissManager(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["StatusMessage"] = "Error: Invalid manager id.";
                return RedirectToAction("Index", "Home");
            }

            // --- Get the two user objects needed for the email logic ---
            var companyAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name); // The admin performing the action
            var managerToDismiss = await _userRepository.GetUserByIdAsync(userId); // The manager being dismissed
            if (managerToDismiss == null || companyAdmin == null)
            {
                return NotFound();
            }

            // Find current assignment for this manager
            var currentAssignment = await _condominiumRepository.GetCondominiumByManagerIdAsync(userId);
            if (currentAssignment == null)
            {
                TempData["StatusMessage"] = "No assignment found for this manager.";
                // Back to the linking page for this manager
                return RedirectToAction(nameof(LinkManagerToCondominium), new { id = userId });
            }

            // Unassign
            currentAssignment.CondominiumManagerId = null;
            _condominiumRepository.Update(currentAssignment);
            await _condominiumRepository.SaveAllAsync();

            TempData["StatusMessage"] = $"Manager has been dismissed from '{currentAssignment.Name}'.";

            // --- START: EMAIL LOGIC ---

            // 1. Send Email to the Manager being dismissed
            await _emailSender.SendEmailAsync(managerToDismiss.Email,
                "Notification: You Have Been Dismissed from Condominium",
                $"<p>This is an automated notification to inform you that your account has been dismissed from managing the condominium '{currentAssignment.Name}' by your company administrator.</p>");

            // 2. Send Confirmation Email to the Company Admin who did it
            await _emailSender.SendEmailAsync(companyAdmin.Email,
                $"Confirmation: Manager {managerToDismiss.FirstName} {managerToDismiss.LastName} Dismissed",
                $"<p>You have successfully dismissed {managerToDismiss.FirstName} {managerToDismiss.LastName} from managing the condominium '{currentAssignment.Name}'.</p>");

            // --- END: EMAIL LOGIC ---

            // Return to the users list for the company
            return RedirectToAction("AllUsersByCompany", new { id = currentAssignment.CompanyId });
        }

        /// <summary>
        /// Activates a previously deactivated Condominium Manager's account.
        /// This action removes the user's lockout, clears their deactivation status,
        /// and sends email notifications to both the activated manager and the admin who performed the action.
        /// </summary>
        /// <param name="id">The ID of the user to activate.</param>
        /// <returns>A redirect to the company's user list.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> ActivateCondominiumManager(string id)
        {
            // Get both users involved first
            var companyAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            var userToActivate = await _userRepository.GetUserByIdAsync(id);

            if (userToActivate == null || companyAdmin == null)
            {
                TempData["StatusMessage"] = "Error: User not found.";
                return NotFound();
            }

            // Check if the user is a Condominium Manager
            if (!await _userManager.IsInRoleAsync(userToActivate, "Condominium Manager"))
            {
                TempData["StatusMessage"] = "Error: This user is not a Condominium Manager.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = userToActivate.CompanyId });
            }

            // You can't activate the currently logged-in user from this view
            if (userToActivate.Id == companyAdmin.Id)
            {
                TempData["StatusMessage"] = "Error: You cannot activate your own account from this panel.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = userToActivate.CompanyId });
            }

            // Reactivate the user
            userToActivate.DeactivatedAt = null;
            userToActivate.DeactivatedByUserId = null;
            userToActivate.UpdatedAt = DateTime.UtcNow;
            userToActivate.UserUpdatedId = companyAdmin.Id; // Log who did the activation

            // Update the user in the database
            var result = await _userRepository.UpdateUserAsync(userToActivate);
            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "Error activating user.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = userToActivate.CompanyId });
            }

            // Remove any lockout on the user
            await _userRepository.SetLockoutEndDateAsync(userToActivate, null);

            // --- START: SEND EMAIL LOGIC ---

            // 1. Send email to the Manager being activated
            await _emailSender.SendEmailAsync(userToActivate.Email,
                "Your Account Has Been Reactivated",
                $"<p>This is an automated notification to inform you that your account ({userToActivate.Email}) has been reactivated by your Company Administrator ({companyAdmin.FirstName} {companyAdmin.LastName}).</p>");

            // 2. Send confirmation email to the Company Admin
            await _emailSender.SendEmailAsync(companyAdmin.Email,
                $"Confirmation: User {userToActivate.FirstName} {userToActivate.LastName} Activated",
                $"<p>You have successfully reactivated the account for manager: {userToActivate.FirstName} {userToActivate.LastName} ({userToActivate.Email}).</p>");

            // --- END: Send EMAIL LOGIC ---

            TempData["StatusMessage"] = $"Manager '{userToActivate.FirstName} {userToActivate.LastName}' has been successfully activated and notified via email.";

            return RedirectToAction(nameof(AllUsersByCompany), new { id = userToActivate.CompanyId });
        }

        /// <summary>
        /// Deactivates a Condominium Manager's account, preventing them from logging in.
        /// This action sends email notifications to both the deactivated manager and the admin who performed the action.
        /// </summary>
        /// <remarks>
        /// This action includes a business rule that prevents deactivation if the manager is still assigned to a condominium.
        /// </remarks>
        /// <param name="id">The ID of the user to deactivate.</param>
        /// <returns>A redirect to the company's user list.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> DeactivateCondominiumManager(string id)
        {
            // Get both users involved first
            var companyAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            var userToDeactivate = await _userRepository.GetUserByIdAsync(id);

            if (userToDeactivate == null || companyAdmin == null)
            {
                TempData["StatusMessage"] = "Error: User not found.";
                return NotFound();
            }

            // Check if the user is a Condominium Manager
            if (!await _userManager.IsInRoleAsync(userToDeactivate, "Condominium Manager"))
            {
                TempData["StatusMessage"] = "Error: This user is not a Condominium Manager.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = userToDeactivate.CompanyId });
            }

            // Check if the manager is currently assigned to a condominium.
            var currentAssignment = await _condominiumRepository.GetCondominiumByManagerIdAsync(id);

            if (currentAssignment != null)
            {
                TempData["StatusMessage"] = $"Error: The manager must be dismissed from '{currentAssignment.Name}' Condominium, before deactivation.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = userToDeactivate.CompanyId });
            }

            // You can't deactivate yourself from this panel
            if (userToDeactivate.Id == companyAdmin.Id)
            {
                TempData["StatusMessage"] = "Error: You cannot deactivate your own account from this panel.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = userToDeactivate.CompanyId });
            }

            // Proceed with deactivation
            userToDeactivate.DeactivatedAt = DateTime.UtcNow;
            userToDeactivate.DeactivatedByUserId = companyAdmin.Id;
            userToDeactivate.UpdatedAt = DateTime.UtcNow;
            userToDeactivate.UserUpdatedId = companyAdmin.Id;

            // Update the user in the database
            var result = await _userRepository.UpdateUserAsync(userToDeactivate);

            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "Error deactivating user.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = userToDeactivate.CompanyId });
            }

            // Lock the user out indefinitely
            await _userRepository.SetLockoutEndDateAsync(userToDeactivate, DateTimeOffset.MaxValue);

            // --- START: Send EMAIL LOGIC ---

            // 1. Send email to the Manager being deactivated
            await _emailSender.SendEmailAsync(userToDeactivate.Email,
                "Your Account Has Been Deactivated",
                $"<p>This is an automated notification to inform you that your account ({userToDeactivate.Email}) has been deactivated by your Company Administrator ({companyAdmin.FirstName} {companyAdmin.LastName}).</p>");

            // 2. Send confirmation email to the Company Admin
            await _emailSender.SendEmailAsync(companyAdmin.Email,
                $"Confirmation: User {userToDeactivate.FirstName} {userToDeactivate.LastName} Deactivated",
                $"<p>You have successfully deactivated the account for manager: {userToDeactivate.FirstName} {userToDeactivate.LastName} ({userToDeactivate.Email}).</p>");

            // --- END: SEND EMAIL LOGIC ---

            TempData["StatusMessage"] = $"Manager '{userToDeactivate.FirstName} {userToDeactivate.LastName}' has been successfully deactivated and notified via email.";

            return RedirectToAction(nameof(AllUsersByCompany), new { id = userToDeactivate.CompanyId });
        }

        /// <summary>
        /// Displays a dedicated page listing all inactive Condominium Managers for a specific company.
        /// </summary>
        /// <param name="id">The ID of the company.</param>
        /// <returns>A view populated with a list of inactive manager accounts.</returns>
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> InactiveManagers(int id) // 'id' is the CompanyId
        {
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            var inactiveUsers = await _userRepository.GetInactiveUsersByCompanyAndRoleAsync(id, "Condominium Manager");

            // We use a foreach loop to build the list, allowing us to fetch roles for each user.
            var userViewModelList = new List<ApplicationUserViewModel>();
            foreach (var user in inactiveUsers)
            {
                userViewModelList.Add(new ApplicationUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName,
                    IsDeactivated = user.DeactivatedAt.HasValue,
                    Roles = await _userRepository.GetUserRolesAsync(user) // <-- This fetches the roles
                });
            }

            var model = new ManagersListViewModel
            {
                AllUsers = userViewModelList
            };

            ViewBag.CompanyId = id;
            ViewBag.CompanyName = company.Name;
            ViewBag.Title = "Inactive Condominium Managers";

            return View(model);
        }

        /// <summary>
        /// Shows the Create Staff form for both Company Admins and Condominium Managers.
        /// Admins can pick any active condo in the company; Managers are locked to their assigned condo.
        /// </summary>
        /// <param name="companyId">Company the staff will belong to (required for admin path).</param>
        /// <returns>View pre-populated with proper condo choices based on the caller's role.</returns>
        [Authorize(Roles = "Company Administrator, Condominium Manager")]
        [HttpGet]
        public async Task<IActionResult> CreateStaff(int companyId)
        {
            // Current user + guard
            var currentUser = await _userRepository.GetUserByEmailasync(User.Identity!.Name);
            if (currentUser == null) return RedirectToAction("Index", "Home");

            // Company context (admin hits this route; for managers it's still fine to validate UI)
            var company = await _companyRepository.GetByIdAsync(companyId);
            if (company == null) return NotFound();

            var roles = await _userRepository.GetUserRolesAsync(currentUser);

            var vm = new RegisterStaffFormViewModel
            {
                CompanyId = companyId
            };

            if (roles.Contains("Company Administrator"))
            {
                // Admin: choose any ACTIVE condo in this company
                var condos = await _condominiumRepository.GetActiveCondominiumsByCompanyIdAsync(companyId);

                vm.CondominiumsList = condos.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
                vm.CanPickCondominium = true; // enable dropdown in UI

                // <-- Cancel: back to the company staff list
                ViewBag.CancelUrl = Url.Action(nameof(AllCondominiumStaffByCompany), "Account", new { id = companyId });
            }
            else
            {
                // Manager: must be assigned to exactly one condo (your 1:1 rule)
                var assigned = await _condominiumRepository.GetCondominiumByManagerIdAsync(currentUser.Id);
                if (assigned == null)
                {
                    TempData["StatusMessage"] = "You must be assigned to a condominium before creating staff.";
                    return RedirectToAction(nameof(AllUsersByCompany), new { id = companyId });
                }

                // Lock selection to the manager's single condo
                vm.CondominiumId = assigned.Id;
                vm.CondominiumsList = new[]
                {
                    new SelectListItem { Value = assigned.Id.ToString(), Text = assigned.Name }
                };
                vm.CanPickCondominium = false;            // dropdown disabled/hidden in UI
                vm.SelectedCondominiumName = assigned.Name; // optional display-only label

                // <-- Cancel: back to the company staff list
                ViewBag.CancelUrl = Url.Action(nameof(AllCondominiumStaffByCompany), "Account", new { id = companyId });
            }

            // Page chrome
            ViewBag.CompanyId = company.Id;
            ViewBag.CompanyName = company.Name;

            return View(vm);
        }

        /// <summary>
        /// Creates a Condominium Staff account respecting role scope:
        /// - Admins can select any active condo in their company.
        /// - Managers are restricted to their assigned condo.
        /// - Sends email confirmation to new staff and notifications to managers/admins.
        /// </summary>
        /// <param name="model">Form values.</param>
        [Authorize(Roles = "Company Administrator,Condominium Manager")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStaff(RegisterStaffFormViewModel model)
        {
            var currentUser = await _userRepository.GetUserByEmailasync(User.Identity!.Name);
            if (currentUser == null) return RedirectToAction("Index", "Home");

            var roles = await _userRepository.GetUserRolesAsync(currentUser);
            var isAdmin = roles.Contains("Company Administrator");
            var isManager = roles.Contains("Condominium Manager");

            // --- Get Company (Needed for validation and email) ---
            var company = await _companyRepository.GetByIdAsync(model.CompanyId);
            if (company == null)
            {
                ModelState.AddModelError(string.Empty, "Company not found.");
                // Should return here, but need xto rehydrate the VM first (which the logic below does)
            }

            // —— Rehydrate condo choices and enforce role scope ——
            if (isAdmin)
            {
                var condos = await _condominiumRepository.GetActiveCondominiumsByCompanyIdAsync(model.CompanyId);
                model.CondominiumsList = condos.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name });
                model.CanPickCondominium = true;

                if (company != null && !condos.Any(c => c.Id == model.CondominiumId))
                    ModelState.AddModelError(nameof(model.CondominiumId), "Please choose a valid condominium.");

                ViewBag.CancelUrl = Url.Action(nameof(AllCondominiumStaffByCompany), "Account", new { id = model.CompanyId });
            }
            else if (isManager)
            {
                var assigned = await _condominiumRepository.GetCondominiumByManagerIdAsync(currentUser.Id);
                if (assigned == null)
                {
                    TempData["StatusMessage"] = "You must be assigned to a condominium before creating staff.";
                    return RedirectToAction(nameof(AllUsersByCompany), new { id = currentUser.CompanyId });
                }

                if (model.CondominiumId != assigned.Id)
                    ModelState.AddModelError(nameof(model.CondominiumId), "You can only assign staff to your condominium.");

                model.CompanyId = currentUser.CompanyId ?? model.CompanyId;
                model.CondominiumsList = new[] { new SelectListItem { Value = assigned.Id.ToString(), Text = assigned.Name } };
                model.CanPickCondominium = false;
                model.SelectedCondominiumName = assigned.Name;
                ViewBag.CancelUrl = Url.Action(nameof(AllCondominiumStaffByCompany), "Account", new { id = model.CompanyId });
            }
            else
            {
                return Forbid();
            }

            // —— Uniqueness guard (email as username) ——
            var existing = await _userManager.FindByNameAsync(model.Username);
            if (existing != null)
            {
                ModelState.AddModelError(nameof(model.Username), "This email is already in use.");
            }

            if (!ModelState.IsValid)
            {
                // Repopulate ViewBag properties if validation fails
                ViewBag.CompanyId = company?.Id;
                ViewBag.CompanyName = company?.Name;
                return View(model);
            }

            // —— Map VM -> ApplicationUser ——
            var user = new ApplicationUser
            {
                FirstName = model.FirstName,
                LastName = model.LastName,
                UserName = model.Username,
                Email = model.Username,
                PhoneNumber = model.PhoneNumber,
                DocumentType = model.DocumentType,
                IdentificationDocument = model.IdentificationDocument,
                Profession = model.Profession,
                CompanyId = model.CompanyId,
                CondominiumId = model.CondominiumId,
                EmailConfirmed = false, // Force confirmation
                CreatedAt = DateTime.UtcNow,
                UserCreatedId = currentUser.Id // Use the full user object ID
            };

            var result = await _userRepository.AddUserAsync(user, model.Password);
            if (!result.Succeeded)
            {
                foreach (var err in result.Errors)
                    ModelState.AddModelError(string.Empty, err.Description);

                // Repopulate ViewBag properties if user creation fails
                ViewBag.CompanyId = company.Id;
                ViewBag.CompanyName = company.Name;
                return View(model);
            }

            await _userRepository.AddUserToRoleAsync(user, "Condominium Staff");


            // --- START: EMAIL LOGIC  ---

            var condo = await _condominiumRepository.GetByIdAsync(model.CondominiumId);

            // 1. Send CONFIRMATION Email to the NEW STAFF member
            var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
            var confirmationLink = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, token }, Request.Scheme);
            await _emailSender.SendEmailAsync(user.Email,
                "Confirm your new CondoManagerPrime Account",
                $"<p>An account has been created for you. Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a></p>");

            // 2. Send NOTIFICATION Email to the ADMIN/MANAGER (who created the account)
            await _emailSender.SendEmailAsync(currentUser.Email,
                $"New Staff Created: {user.FirstName} {user.LastName}",
                $"<p>You have successfully created a new staff account for {user.FirstName} {user.LastName} ({user.Email}) for the condominium {condo?.Name}.</p>");

            // 3. Send NOTIFICATION Email to the main COMPANY ADMINISTRATOR 
            var companyAdmin = await _userRepository.GetUserByIdAsync(company.ApplicationUserId);
            if (companyAdmin != null)
            {
                await _emailSender.SendEmailAsync(companyAdmin.Email,
                    $"New Staff Added to Company: {user.FirstName} {user.LastName}",
                    $"<p>This is a notification that your user ({currentUser.FirstName} {currentUser.LastName}) has created a new staff account:</p>" +
                    $"<ul><li><b>Staff:</b> {user.FirstName} {user.LastName} ({user.Email})</li><li><b>Condominium:</b> {condo?.Name}</li><li><b>Company:</b> {company.Name}</li></ul>");
            }
            // --- END: EMAIL LOGIC ---


            TempData["StatusMessage"] = $"Staff member {user.FirstName} {user.LastName} created. A confirmation email has been sent.";
            return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = model.CompanyId });
        }

        /// <summary>
        /// Lists company staff (active by default; pass <paramref name="showInactive"/> to see deactivated).
        /// Uses the shared builder helper to keep logic centralized.
        /// </summary>
        /// <param name="id">Company ID.</param>
        /// <param name="showInactive">If true, shows deactivated staff members.</param>
        /// <returns>Staff list view.</returns>
        [Authorize(Roles = "Company Administrator,Condominium Manager")]
        public async Task<IActionResult> AllCondominiumStaffByCompany(int id, bool showInactive = false)
        {
            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (loggedInUser == null) return RedirectToAction("Index", "Home");

            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null) return NotFound();

            // Set view context data (keeps UI consistent with Managers page)
            ViewBag.CompanyName = company.Name;
            ViewBag.CompanyId = company.Id;
            ViewBag.Title = showInactive ? "Inactive Condominium Staff" : "Condominium Staff";
            ViewBag.ShowingInactive = showInactive;

            // Use the helper (filters by role and resolves condo names)
            var list = await BuildUserListByRoleAsync(id, "Condominium Staff", showInactive);

            var model = new StaffListViewModel { AllUsers = list };
            return View(model);
        }

        /// <summary>
        /// Activates a previously deactivated Condominium Staff account.
        /// Sends notifications to the staff member, their assigned manager, and the company admin.
        /// </summary>
        /// <param name="id">Staff user ID.</param>
        /// <returns>Redirects back to the staff list for the user's company.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")] // Only Admins can reactivate accounts
        public async Task<IActionResult> ActivateCondominiumStaff(string id)
        {
            var userToActivate = await _userRepository.GetUserByIdAsync(id);
            if (userToActivate == null) return NotFound();

            var companyAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (companyAdmin == null) return Unauthorized();

            // Guard: ensure the user is actually a staff member.
            if (!await _userManager.IsInRoleAsync(userToActivate, "Condominium Staff"))
            {
                TempData["StatusMessage"] = "Error: This user is not Condominium Staff.";
                return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = userToActivate.CompanyId });
            }

            // Reactivate the user
            userToActivate.DeactivatedAt = null;
            userToActivate.DeactivatedByUserId = null;
            userToActivate.UpdatedAt = DateTime.UtcNow;
            userToActivate.UserUpdatedId = companyAdmin.Id;

            var result = await _userRepository.UpdateUserAsync(userToActivate);
            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "Error activating staff.";
                return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = userToActivate.CompanyId, showInactive = true });
            }

            // Remove lockout
            await _userRepository.SetLockoutEndDateAsync(userToActivate, null);

            // --- START EMAIL LOGIC ---
            // 1. Notify Staff
            await _emailSender.SendEmailAsync(userToActivate.Email, "Your Account Has Been Reactivated",
                $"<p>Your account ({userToActivate.Email}) has been reactivated by your Company Administrator.</p>");

            // 2. Notify Company Admin (the one who did it)
            await _emailSender.SendEmailAsync(companyAdmin.Email, $"Confirmation: Staff {userToActivate.FirstName} Activated",
                $"<p>You have successfully reactivated the account for staff member: {userToActivate.FirstName} {userToActivate.LastName}.</p>");

            // 3. Notify the Condo Manager (if they are assigned and are a different person)
            if (userToActivate.CondominiumId.HasValue)
            {
                var condo = await _condominiumRepository.GetByIdAsync(userToActivate.CondominiumId.Value);
                if (condo != null && !string.IsNullOrEmpty(condo.CondominiumManagerId) && condo.CondominiumManagerId != companyAdmin.Id)
                {
                    var condoManager = await _userRepository.GetUserByIdAsync(condo.CondominiumManagerId);
                    if (condoManager != null)
                    {
                        await _emailSender.SendEmailAsync(condoManager.Email, $"Staff Account Reactivated: {userToActivate.FirstName}",
                            $"<p>This is to notify you that the staff account for {userToActivate.FirstName} {userToActivate.LastName} (assigned to your condominium) has been reactivated by the Company Admin.</p>");
                    }
                }
            }
            // --- END EMAIL LOGIC ---

            TempData["StatusMessage"] = $"Staff '{userToActivate.FirstName} {userToActivate.LastName}' activated.";
            // Send them back to the INACTIVE list (where the user will now be gone)
            return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = userToActivate.CompanyId, showInactive = true });
        }

        /// <summary>
        /// Deactivates an active Condominium Staff account (soft lock + audit).
        /// This action can only be performed on UNASSIGNED staff.
        /// Sends notifications to the staff member, the admin performing the action, and the staff member's original creator.
        /// </summary>
        /// <param name="id">Staff user ID.</param>
        /// <returns>Redirects back to the staff list for the user's company.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> DeactivateCondominiumStaff(string id)
        {
            var userToDeactivate = await _userRepository.GetUserByIdAsync(id);
            if (userToDeactivate == null) return NotFound();

            var companyAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (companyAdmin == null) return Unauthorized();

            if (!await _userManager.IsInRoleAsync(userToDeactivate, "Condominium Staff"))
            {
                TempData["StatusMessage"] = "Error: This user is not Condominium Staff.";
                return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = userToDeactivate.CompanyId });
            }

            if (userToDeactivate.Id == companyAdmin.Id)
            {
                TempData["StatusMessage"] = "Error: You cannot deactivate your own account.";
                return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = userToDeactivate.CompanyId });
            }

            // Business rule: Only allow deactivation of unassigned staff.
            if (userToDeactivate.CondominiumId.HasValue)
            {
                var condo = await _condominiumRepository.GetByIdAsync(userToDeactivate.CondominiumId.Value);
                TempData["StatusMessage"] = $"Error: Staff member must be dismissed from '{condo?.Name}' before they can be deactivated.";
                return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = userToDeactivate.CompanyId });
            }

            // Proceed with deactivation
            userToDeactivate.DeactivatedAt = DateTime.UtcNow;
            userToDeactivate.DeactivatedByUserId = companyAdmin.Id;
            userToDeactivate.UpdatedAt = DateTime.UtcNow;
            userToDeactivate.UserUpdatedId = companyAdmin.Id;

            var result = await _userRepository.UpdateUserAsync(userToDeactivate);
            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "Error deactivating staff.";
                return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = userToDeactivate.CompanyId });
            }

            await _userRepository.SetLockoutEndDateAsync(userToDeactivate, DateTimeOffset.MaxValue);

            // --- START: All EMAIL LOGIC ---

            // 1. Notify Staff (The user being deactivated)
            await _emailSender.SendEmailAsync(userToDeactivate.Email, "Your Account Has Been Deactivated",
                $"<p>Your account ({userToDeactivate.Email}) has been deactivated by your Company Administrator.</p>");

            // 2. Notify Company Admin (The one who performed the action)
            await _emailSender.SendEmailAsync(companyAdmin.Email, $"Confirmation: Staff {userToDeactivate.FirstName} Deactivated",
                $"<p>You have successfully deactivated the account for unassigned staff member: {userToDeactivate.FirstName} {userToDeactivate.LastName}.</p>");

            // 3. Notify the Staff's Creator (who is likely their Manager), if they are a different person.
            if (!string.IsNullOrEmpty(userToDeactivate.UserCreatedId) && userToDeactivate.UserCreatedId != companyAdmin.Id)
            {
                var creator = await _userRepository.GetUserByIdAsync(userToDeactivate.UserCreatedId);
                if (creator != null)
                {
                    await _emailSender.SendEmailAsync(creator.Email,
                        $"Account Deactivated: {userToDeactivate.FirstName}",
                        $"<p>This is a notification that the staff account for {userToDeactivate.FirstName} {userToDeactivate.LastName} (which you created) has been deactivated by the Company Administrator.</p>");
                }
            }
            // --- END: All EMAIL LOGIC ---

            TempData["StatusMessage"] = $"Staff '{userToDeactivate.FirstName} {userToDeactivate.LastName}' deactivated.";
            return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = userToDeactivate.CompanyId });
        }

        /// <summary>
        /// (POST) Assigns an unassigned staff member to a selected condominium.
        /// Notifies the staff member, their new manager, and the company admin.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")] // Only Admin can assign staff
        public async Task<IActionResult> AssignStaffToCondominium(LinkManagerToCondominiumViewModel model)
        {
            var staffUser = await _userRepository.GetUserByIdAsync(model.UserId);
            var companyAdmin = await _userRepository.GetUserByEmailasync(User.Identity.Name);

            if (staffUser == null || companyAdmin == null) return NotFound();

            if (model.SelectedCondominiumId == null || model.SelectedCondominiumId == 0)
            {
                ModelState.AddModelError(nameof(model.SelectedCondominiumId), "You must select a condominium.");
                // Rehydrate the model and return the view
                model.CondominiumsList = await _condominiumRepository.GetUnassignedCondominiumsByCompanyAdminAsync(companyAdmin.Id);
                model.FullName = $"{staffUser.FirstName} {staffUser.LastName}";
                model.CompanyId = staffUser.CompanyId ?? 0;
                return View(model);
            }

            var condoToAssign = await _condominiumRepository.GetByIdAsync(model.SelectedCondominiumId.Value);
            if (condoToAssign == null) return NotFound();

            // --- Perform Assignment ---
            staffUser.CondominiumId = condoToAssign.Id;
            await _userRepository.UpdateUserAsync(staffUser);

            // --- START EMAIL LOGIC ---
            // 1. Notify Staff
            await _emailSender.SendEmailAsync(staffUser.Email, "You Have a New Assignment",
                $"<p>This is a notification to inform you that you have been assigned to the condominium: <strong>{condoToAssign.Name}</strong>.</p>");

            // 2. Notify Company Admin (the one who did it)
            await _emailSender.SendEmailAsync(companyAdmin.Email, $"Confirmation: Staff Assigned: {staffUser.FirstName}",
                $"<p>You have successfully assigned {staffUser.FirstName} {staffUser.LastName} to the condominium '{condoToAssign.Name}'.</p>");

            // 3. Notify the new Condo Manager
            if (!string.IsNullOrEmpty(condoToAssign.CondominiumManagerId))
            {
                var condoManager = await _userRepository.GetUserByIdAsync(condoToAssign.CondominiumManagerId);
                if (condoManager != null)
                {
                    await _emailSender.SendEmailAsync(condoManager.Email, $"New Staff Assigned to your Condominium",
                        $"<p>This is to notify you that a new staff member, {staffUser.FirstName} {staffUser.LastName}, has been assigned to your condominium ('{condoToAssign.Name}').</p>");
                }
            }
            // --- END EMAIL LOGIC ---

            TempData["StatusMessage"] = $"Staff member {staffUser.FirstName} {staffUser.LastName} has been assigned to {condoToAssign.Name}.";
            return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = staffUser.CompanyId });
        }

        /// <summary>
        /// (POST) Dismisses a staff member from a condominium (sets their CondoId to null).
        /// Notifies the staff member, their (now former) manager, and the company admin.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator, Condominium Manager")] // Can be done by Admin or their direct Manager
        public async Task<IActionResult> DismissStaff(string id)
        {
            var staffUser = await _userRepository.GetUserByIdAsync(id);
            if (staffUser == null || !staffUser.CondominiumId.HasValue)
            {
                return NotFound(); // Can't dismiss someone who isn't assigned
            }

            var performer = await _userRepository.GetUserByEmailasync(User.Identity.Name); // Person clicking the button
            var condo = await _condominiumRepository.GetByIdAsync(staffUser.CondominiumId.Value);
            if (condo == null) return NotFound(); // Safety check

            var company = await _companyRepository.GetByIdAsync(staffUser.CompanyId.Value);
            var mainCompanyAdmin = await _userRepository.GetUserByIdAsync(company.ApplicationUserId);

            // Get the manager of the condo they are being dismissed from
            ApplicationUser condoManager = null;
            if (!string.IsNullOrEmpty(condo.CondominiumManagerId))
            {
                condoManager = await _userRepository.GetUserByIdAsync(condo.CondominiumManagerId);
            }

            // --- Perform Dismissal ---
            staffUser.CondominiumId = null; // Un-assign them
            await _userRepository.UpdateUserAsync(staffUser);

            TempData["StatusMessage"] = $"Staff member {staffUser.FirstName} {staffUser.LastName} has been dismissed and is now Unassigned.";

            // --- START: UPDATED EMAIL LOGIC ---

            // Get the role of the person performing the action for the email log
            var performerRoles = await _userManager.GetRolesAsync(performer);
            var performerRole = performerRoles.FirstOrDefault() ?? "Management"; // Default to 'Management' if no specific role

            // 1. Notify Staff
            await _emailSender.SendEmailAsync(staffUser.Email, "You Have Been Dismissed From Condominium",
                $"<p>This is a notification to inform you that your account has been dismissed (unassigned) from the condominium '{condo.Name}' by your {performerRole}.</p>");

            // 2. Notify the Manager they were dismissed from (if manager exists and wasn't the one who dismissed them)
            if (condoManager != null && condoManager.Id != performer.Id)
            {
                await _emailSender.SendEmailAsync(condoManager.Email, $"Staff Dismissed: {staffUser.FirstName}",
                    $"<p>This is to notify you that the staff member {staffUser.FirstName} {staffUser.LastName} has been dismissed from your condominium '{condo.Name}' by the {performerRole}.</p>");
            }

            // 3. Notify the Main Company Admin (if they weren't the one who dismissed them)
            // (The email to the main admin should show *who* did the action, so we keep the name here for the audit trail)
            if (mainCompanyAdmin != null && mainCompanyAdmin.Id != performer.Id)
            {
                await _emailSender.SendEmailAsync(mainCompanyAdmin.Email, $"Record: Staff Dismissed: {staffUser.FirstName}",
                     $"<p>This is a record that the user {performer.FirstName} {performer.LastName} ({performerRole}) dismissed staff member {staffUser.FirstName} from condominium '{condo.Name}'.</p>");
            }
            // --- END: UPDATED EMAIL LOGIC ---

            return RedirectToAction(nameof(AllCondominiumStaffByCompany), new { id = staffUser.CompanyId });
        }

        /// <summary>
        /// Company overview page (active users only): shows Managers and Staff tables together.
        /// </summary>
        /// <param name="id">Company ID.</param>
        /// <returns>Overview view with both lists.</returns>
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> CompanyEmployees(int id)
        {
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null) return NotFound();

            // Use the same helper to keep logic centralized; show ACTIVE only on the overview.
            var managers = await BuildUserListByRoleAsync(id, "Condominium Manager", showInactive: false);
            var staff = await BuildUserListByRoleAsync(id, "Condominium Staff", showInactive: false);

            var vm = new CompanyUsersViewModel
            {
                CompanyId = company.Id,
                CompanyName = company.Name,
                Managers = managers,
                Staff = staff
            };

            ViewBag.CompanyId = company.Id;
            ViewBag.CompanyName = company.Name;
            return View(vm);
        }

        /// <summary>
        /// Displays read-only details for a Staff member.
        /// Secures the action to ensure only an Admin from the same company
        /// or the staff's direct Manager can view their details.
        /// </summary>
        /// <param name="id">The User ID of the staff member.</param>
        /// <returns>A view with the staff member's details.</returns>
        [Authorize(Roles = "Company Administrator, Condominium Manager")]
        public async Task<IActionResult> StaffDetails(string id)
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
                return RedirectToAction("AccessDenied", "Error");
            }

            // Get Condo and Company names to display in the view
            var condo = await _condominiumRepository.GetByIdAsync(staffMember.CondominiumId.Value);
            var company = await _companyRepository.GetByIdAsync(staffMember.CompanyId.Value);

            ViewBag.CondominiumName = condo?.Name ?? "N/A";
            ViewBag.CompanyName = company?.Name ?? "N/A";

            // Pass the ApplicationUser entity to your existing CondominiumStaff/Details view
            return View("~/Views/CondominiumStaff/Detail.cshtml", staffMember);
        }

        /// <summary>
        /// Displays a dedicated page listing all inactive Condominium Staff for a specific company.
        /// </summary>
        /// <param name="id">The ID of the company (passed as CompanyId).</param>
        /// <returns>A view populated with a list of inactive staff accounts.</returns>
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> InactiveStaff(int id) // 'id' is the CompanyId
        {
            var company = await _companyRepository.GetByIdAsync(id);
            if (company == null)
            {
                return NotFound();
            }

            // Call your existing helper, filtering for "Condominium Staff" and forcing showInactive: true
            var inactiveStaffList = await BuildUserListByRoleAsync(id, "Condominium Staff", showInactive: true);

            var model = new StaffListViewModel // Use your existing StaffListViewModel
            {
                AllUsers = inactiveStaffList
            };

            ViewBag.CompanyId = id;
            ViewBag.CompanyName = company.Name;
            ViewBag.Title = "Inactive Condominium Staff";

            // We will now create this new View file
            return View(model);
        }

        /// <summary>
        /// Builds a user list for a company filtered by a specific role (e.g., "Condominium Manager" or "Condominium Staff").
        /// </summary>
        /// <param name="companyId">Company ID.</param>
        /// <param name="role">Exact role name to include.</param>
        /// <param name="showInactive">If true, include deactivated users; otherwise only active.</param>
        /// <returns>Table-ready list of users.</returns>
        private async Task<List<ApplicationUserViewModel>> BuildUserListByRoleAsync(
            int companyId, string role, bool showInactive)
        {
            // Reuse existing repo methods, then filter by role (keeps this self-contained).
            IEnumerable<ApplicationUser> users = showInactive
                ? await _userRepository.GetInactiveUsersByCompanyIdAsync(companyId)
                : await _userRepository.GetActiveUsersByCompanyIdAsync(companyId);

            var list = new List<ApplicationUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userRepository.GetUserRolesAsync(user);
                if (!roles.Contains(role)) continue; // Keep only requested role

                string? assignedCondoName = null;

                // Managers: assignment via one-to-one (manager -> condominium)
                if (role == "Condominium Manager")
                {
                    var assignment = await _condominiumRepository.GetCondominiumByManagerIdAsync(user.Id);
                    assignedCondoName = assignment?.Name;
                }
                // Staff: assignment via user's CondominiumId
                else if (role == "Condominium Staff" && user.CondominiumId.HasValue)
                {
                    var condo = await _condominiumRepository.GetByIdAsync(user.CondominiumId.Value);
                    assignedCondoName = condo?.Name;
                }

                list.Add(new ApplicationUserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    UserName = user.UserName,
                    IsDeactivated = user.DeactivatedAt.HasValue,
                    Roles = roles,
                    AssignedCondominiumName = assignedCondoName,
                    IsEmailConfirmed = user.EmailConfirmed
                });
            }

            return list;
        }



        // TODO: Future feature - account deactivation by user themselves?
        /* ------------------------ This code below is left commented on purpose ---------------*/

        ///// <summary>
        ///// Displays the confirmation page for account deactivation.
        ///// </summary>
        //[Authorize]
        //public IActionResult DeactivateMyAccount()
        //{
        //    return View();
        //}

        //[HttpPost, ActionName("DeactivateMyAccount")]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> DeactivateMyAccountConfirmed()
        //{
        //    // As per your new rule, only the Platform Admin can use this feature.
        //    if (!User.IsInRole("Platform Administrator"))
        //    {
        //        TempData["StatusMessage"] = "Error: You do not have permission to perform this action.";
        //        return RedirectToAction(nameof(AccountDetails));
        //    }

        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null)
        //    {
        //        return NotFound();
        //    }

        //    // Deactivate the current user
        //    user.DeactivatedAt = DateTime.UtcNow;
        //    user.DeactivatedByUserId = user.Id; // They did it to themselves
        //    await _userRepository.UpdateUserAsync(user);
        //    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

        //    await _signInManager.SignOutAsync();
        //    return RedirectToAction("Index", "Home");
        //}
    }
}