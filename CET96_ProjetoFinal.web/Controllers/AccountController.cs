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

        public AccountController(
            IApplicationUserRepository userRepository,
            ICompanyRepository companyRepository,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            UserManager<ApplicationUser> userManager)
        {
            _userRepository = userRepository;
            _companyRepository = companyRepository;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _userManager = userManager;
        }

        // GET: /Account/Login
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

        // POST: /Account/Login
        //[HttpPost]
        //[AllowAnonymous]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var result = await _userRepository.LoginAsync(model);

        //        if (result.Succeeded)
        //        {
        //            // --- CHECK FOR COMPANY ---
        //            var user = await _userRepository.GetUserByEmailasync(model.Username);

        //            // Check if a company exists for this user.
        //            var companyExists = await _companyRepository.DoesCompanyExistForUserAsync(user.Id);

        //            if (!companyExists)
        //            {
        //                // If no company exists, force them to the Create Company page.
        //                return RedirectToAction("Create", "Companies");
        //            }

        //            // If a company exists, proceed to the home page.
        //            return RedirectToAction("Index", "Home");
        //        }
        //        if (result.IsNotAllowed)
        //        {
        //            ModelState.AddModelError(string.Empty, "You must confirm your email before you can log in.");
        //        }
        //        else
        //        {
        //            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        //        }
        //    }
        //    return View(model);
        //}
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
        /// </returns>
        [HttpPost]
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


        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _userRepository.LogoutAsync();
            // After logging out, send the user to the home page.
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/ChangePassword
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // POST: /Account/ChangePassword
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