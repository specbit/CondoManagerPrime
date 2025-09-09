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
        public IActionResult Register()
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
        /// </returns>        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterCompanyAdminViewModel model)
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
                    ModelState.AddModelError(string.Empty, "This email is already in use.");
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

                        //// --- EMAIL CONFIRMATION LOGIC ---
                        //var token = await _userRepository.GenerateEmailConfirmationTokenAsync(user);
                        //var confirmationLink = Url.Action("ConfirmEmail", "Account",
                        //    new { userId = user.Id, token }, Request.Scheme);

                        //await _emailSender.SendEmailAsync(model.Username,
                        //    "Confirm your email for CondoManagerPrime",
                        //    $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a>");
                        //// --- END EMAIL CONFIRMATION LOGIC ---

                        //var modelForView = new RegistrationConfirmationViewModel
                        //{
                        //    ConfirmationLink = confirmationLink
                        //};

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

        /// <summary>
        /// Handles the link clicked by a user from their email to confirm their account.
        /// It validates the user and token, confirms the email, signs the user in, and
        /// redirects them to the company creation flow.
        /// </summary>
        /// <param name="userId">The ID of the user to confirm.</param>
        /// <param name="token">The confirmation token.</param>
        /// <returns>A redirect to the company creation page on success, or an er
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }

            var result = await _userRepository.ConfirmEmailAsync(user, token);

            if (result.Succeeded)
            {
                // 1. Sign the user in to create a session.
                await _signInManager.SignInAsync(user, isPersistent: false);

                // 2. CRUCIAL STEP: Refresh the sign-in session. This forces the user's
                // roles and claims to be reloaded into the cookie immediately.
                await _signInManager.RefreshSignInAsync(user);

                // 3. Now that the session is guaranteed to be valid and have the correct roles,
                // check if they have a company.
                var companyExists = await _companyRepository.DoesCompanyExistForUserAsync(user.Id);

                if (!companyExists)
                {
                    // The redirect will now succeed because the user is properly authenticated WITH their roles.
                    //return RedirectToAction("Create", "Companies");
                    return RedirectToAction("Create", "Companies", new { companyName = user.CompanyName });
                }

                // If for some reason they already have a company, send them home.
                return RedirectToAction("Index", "Home");
            }

            // If confirmation fails, show an error.
            return View("Error");
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

        // GET: All Users By Company Administrator
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> AllUsersByCompany(int id, bool showInactive = false)
        {
            int companyId = id;

            var loggedInUser = await _userRepository.GetUserByEmailasync(User.Identity.Name);
            if (loggedInUser == null) return RedirectToAction("Index", "Home");

            var company = await _companyRepository.GetByIdAsync(companyId);

            if (company == null)
            {
                return NotFound();
            }

            ViewBag.CompanyName = company?.Name; // Pass the company doesn't thow an exception if null
            ViewBag.CompanyId = company.Id; // Pass the company ID to the view

            // --- NEW FILTERING LOGIC ---
            // We declare the list here and populate it based on the showInactive flag.
            IEnumerable<ApplicationUser> users;
            if (showInactive)
            {
                users = await _userRepository.GetInactiveUsersByCompanyIdAsync(companyId);
                ViewBag.Title = "Inactive Condominium Managers";
                ViewBag.ShowingInactive = true;
            }
            else
            {
                users = await _userRepository.GetActiveUsersByCompanyIdAsync(companyId);
                ViewBag.Title = "Condominium Managers";
                ViewBag.ShowingInactive = false;
            }
            // --- END OF NEW LOGIC ---
            //var users = await _userRepository.GetUsersByCompanyIdAsync(companyId);

            var userViewModelList = new List<ApplicationUserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userRepository.GetUserRolesAsync(user);
                string? assignedCondoName = null;

                // Check ONLY for users who are in the "Condominium Manager" role
                if (roles.Contains("Condominium Manager"))
                {
                    var assignment = await _condominiumRepository.GetCondominiumByManagerIdAsync(user.Id);
                    // If an assignment is found, get its name
                    assignedCondoName = assignment?.Name;

                    userViewModelList.Add(new ApplicationUserViewModel
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        UserName = user.UserName,
                        IsDeactivated = user.DeactivatedAt.HasValue,
                        Roles = roles,
                        AssignedCondominiumName = assignedCondoName
                    });
                }
            }

            var model = new CondominiumManagerViewModel
            {
                AllUsers = userViewModelList
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> LinkManagerToCondominium(LinkManagerToCondominiumViewModel model)
        {
            // 1. Validate the input.
            var condominiumManager = await _userRepository.GetUserByIdAsync(model.UserId);
            int companyId = condominiumManager?.CompanyId ?? 0;

            if (condominiumManager == null)
            {
                TempData["StatusMessage"] = "Error: Condominium Manager not found.";
                return NotFound();
            }

            // Check if the user is deactivated before allowing the assignment.
            if (condominiumManager.DeactivatedAt.HasValue)
            {
                TempData["StatusMessage"] = $"Error: Cannot assign a condominium to a deactivated user. Please activate the account first.";

                // Get the companyId to return to the correct view
                return RedirectToAction(nameof(AllUsersByCompany), new { id = companyId });
            }

            if (model.SelectedCondominiumId == null || model.SelectedCondominiumId == 0)
            {
                // If no condominium was selected, add an error and return to the form.
                ModelState.AddModelError(string.Empty, "You must select a condominium.");

                // Reload the necessary data before returning to the view.
                // Repopulate the model with the data needed to re-render the view.
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

            // 6. Save the changes.
            _condominiumRepository.Update(condominiumToUpdate);
            await _condominiumRepository.SaveAllAsync();

            condominiumManager.CompanyId = condominiumToUpdate.CompanyId;

            await _userRepository.UpdateUserAsync(condominiumManager);

            // IMPORTANT: Get the companyId to pass it back to the AllUsersByCompany action

            TempData["StatusMessage"] = $"Manager has been successfully linked to condominium '{condominiumToUpdate.Name}'.";

            // 7. Redirect back to the list of managers.
            return RedirectToAction("AllUsersByCompany", new { id = condominiumToUpdate.CompanyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> DismissManager(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["StatusMessage"] = "Error: Invalid manager id.";
                return RedirectToAction("Index", "Home");
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

            // Return to the users list for the company
            return RedirectToAction("AllUsersByCompany", new { id = currentAssignment.CompanyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> ActivateCondominiumManager(string id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

            if (user == null)
            {
                TempData["StatusMessage"] = "Error: User not found.";
                return NotFound();
            }

            // Check if the user is a Condominium Manager
            if (!await _userManager.IsInRoleAsync(user, "Condominium Manager"))
            {
                TempData["StatusMessage"] = "Error: This user is not a Condominium Manager.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = user.CompanyId });
            }

            // You can't activate the currently logged-in user from this view
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["StatusMessage"] = "Error: You cannot activate your own account from this panel.";

                return RedirectToAction(nameof(AllUsersByCompany), new { id = user.CompanyId });
            }

            // Reactivate the user
            user.DeactivatedAt = null;
            user.DeactivatedByUserId = null;
            user.UpdatedAt = DateTime.UtcNow;
            user.UserUpdatedId = _userManager.GetUserId(User);

            // Update the user in the database
            var result = await _userRepository.UpdateUserAsync(user);
            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "Error activating user.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = user.CompanyId });
            }

            // Remove any lockout on the user
            await _userRepository.SetLockoutEndDateAsync(user, null);

            TempData["StatusMessage"] = $"Manager '{user.FirstName} {user.LastName}' has been successfully activated.";

            return RedirectToAction(nameof(AllUsersByCompany), new { id = user.CompanyId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Company Administrator")]
        public async Task<IActionResult> DeactivateCondominiumManager(string id)
        {
            var user = await _userRepository.GetUserByIdAsync(id);

            if (user == null)
            {
                TempData["StatusMessage"] = "Error: User not found.";

                return NotFound();
            }

            // Check if the user is a Condominium Manager
            if (!await _userManager.IsInRoleAsync(user, "Condominium Manager"))
            {
                TempData["StatusMessage"] = "Error: This user is not a Condominium Manager.";

                return RedirectToAction(nameof(AllUsersByCompany), new { id = user.CompanyId });
            }

            // Check if the manager is currently assigned to a condominium.
            var currentAssignment = await _condominiumRepository.GetCondominiumByManagerIdAsync(id);

            if (currentAssignment != null)
            {
                TempData["StatusMessage"] = $"Error: The manager must be dismissed from '{currentAssignment.Name}' Condominium, before deactivation.";
                return RedirectToAction(nameof(AllUsersByCompany), new { id = user.CompanyId });
            }

            // You can't deactivate the currently logged-in user from this view
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["StatusMessage"] = "Error: You cannot deactivate your own account from this panel.";

                return RedirectToAction(nameof(AllUsersByCompany), new { id = user.CompanyId });
            }

            // Proceed with deactivation as the manager is not assigned
            user.DeactivatedAt = DateTime.UtcNow;
            user.DeactivatedByUserId = _userManager.GetUserId(User);
            user.UpdatedAt = DateTime.UtcNow;
            user.UserUpdatedId = _userManager.GetUserId(User);

            // Update the user in the database
            var result = await _userRepository.UpdateUserAsync(user);

            if (!result.Succeeded)
            {
                TempData["StatusMessage"] = "Error deactivating user.";

                return RedirectToAction(nameof(AllUsersByCompany), new { id = user.CompanyId });
            }

            // Lock the user out indefinitely
            await _userRepository.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

            TempData["StatusMessage"] = $"Manager '{user.FirstName} {user.LastName}' has been successfully deactivated.";

            return RedirectToAction(nameof(AllUsersByCompany), new { id = user.CompanyId });
        }

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

            var model = new CondominiumManagerViewModel
            {
                AllUsers = userViewModelList
            };

            ViewBag.CompanyId = id;
            ViewBag.CompanyName = company.Name;
            ViewBag.Title = "Inactive Condominium Managers";

            return View(model);
        }

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