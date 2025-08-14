using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Helpers;
using CET96_ProjetoFinal.web.Models;
using CET96_ProjetoFinal.web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CET96_ProjetoFinal.web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IApplicationUserHelper _applicationUserHelper;
        private readonly ApplicationUserDataContext _dataContext;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountController(
            IApplicationUserHelper userHelper,
            ApplicationUserDataContext context,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _applicationUserHelper = userHelper;
            _dataContext = context;
            _signInManager = signInManager;
            _emailSender = emailSender;
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

        //// POST: /Account/Login
        //[HttpPost]
        //[AllowAnonymous]
        //public async Task<IActionResult> Login(LoginViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var result = await _applicationUserHelper.LoginAsync(model);
        //        if (result.Succeeded)
        //        {
        //            return RedirectToAction("Index", "Home");
        //        }

        //        // --- Check if login failed because email is not confirmed ---
        //        if (result.IsNotAllowed)
        //        {
        //            ModelState.AddModelError(string.Empty, "You must confirm your email before you can log in.");
        //        }
        //        else
        //        {
        //            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        //        }
        //    }

        //    ModelState.AddModelError(string.Empty, "Invalid login attempt.");

        //    return View(model);
        //}

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _applicationUserHelper.LoginAsync(model);

                if (result.Succeeded)
                {
                    // --- CHECK FOR COMPANY ---
                    var user = await _applicationUserHelper.GetUserByEmailasync(model.Username);

                    // Check if a company exists for this user.
                    var companyExists = await _dataContext.Companies.AnyAsync(c => c.ApplicationUserId == user.Id);

                    if (!companyExists)
                    {
                        // If no company exists, force them to the Create Company page.
                        return RedirectToAction("Create", "Company");
                    }

                    // If a company exists, proceed to the home page.
                    return RedirectToAction("Index", "Home");
                }
                if (result.IsNotAllowed)
                {
                    ModelState.AddModelError(string.Empty, "You must confirm your email before you can log in.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                }
            }
            return View(model);
        }


        // GET: /Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        //// POST: /Account/Register
        //[HttpPost]
        //[AllowAnonymous]
        //public async Task<IActionResult> Register(RegisterCompanyAdminViewModel model)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        var user = await _applicationUserHelper.GetUserByEmailasync(model.Username);
        //        if (user == null)
        //        {
        //            user = new ApplicationUser
        //            {
        //                FirstName = model.FirstName,
        //                LastName = model.LastName,
        //                UserName = model.Username, // Identity requires UserName
        //                Email = model.Username,
        //                IdentificationDocument = model.IdentificationDocument,
        //                DocumentType = model.DocumentType,
        //                PhoneNumber = model.PhoneNumber
        //            };

        //            // Create the user in the database
        //            var result = await _applicationUserHelper.AddUserAsync(user, model.Password);
        //            if (result.Succeeded)
        //            {
        //                await _applicationUserHelper.AddUserToRoleAsync(user, "Company Administrator");

        //                // Create the associated Company 
        //                var company = new Company
        //                {
        //                    Name = model.CompanyName,
        //                    ApplicationUserId = user.Id, // Link the company to the new user
        //                    UserCreatedId = user.Id,
        //                    PaymentValidated = true // Simulate a successful payment
        //                };

        //                _dataContext.Companies.Add(company);
        //                await _dataContext.SaveChangesAsync();

        //                // Instead of using the helper for this one action, take direct control 
        //                // of the sign-in process to ensure the session is updated immediately.
        //                await _signInManager.SignInAsync(user, isPersistent: false);

        //                // This is the crucial line that forces the login cookie to be recognized.
        //                await _signInManager.RefreshSignInAsync(user);

        //                // Send them to the home page
        //                return RedirectToAction("Index", "Home");
        //            }

        //            // If creation failed, add errors to the page
        //            foreach (var error in result.Errors)
        //            {
        //                ModelState.AddModelError(string.Empty, error.Description);
        //            }
        //        }
        //        else
        //        {
        //            ModelState.AddModelError(string.Empty, "A user with this email already exists.");
        //        }
        //    }

        //    return View(model);
        //}

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterCompanyAdminViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _applicationUserHelper.GetUserByEmailasync(model.Username);
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

                    var result = await _applicationUserHelper.AddUserAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await _applicationUserHelper.AddUserToRoleAsync(user, "Company Administrator");

                        // --- EMAIL CONFIRMATION LOGIC ---
                        var token = await _applicationUserHelper.GenerateEmailConfirmationTokenAsync(user);
                        var confirmationLink = Url.Action("ConfirmEmail", "Account",
                            new { userId = user.Id, token }, Request.Scheme);

                        await _emailSender.SendEmailAsync(model.Username,
                            "Confirm your email for CondoManagerPrime",
                            $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>link</a>");

                        // Show a page telling the user to check their email. NO company is created here.
                        return View("RegistrationConfirmation");
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
            await _applicationUserHelper.LogoutAsync();
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

            var user = await _applicationUserHelper.GetUserByEmailasync(User.Identity.Name);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found.");
                return View(model);
            }

            var result = await _applicationUserHelper.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            TempData["SuccessMessage"] = "Your password has been changed successfully.";

            return RedirectToAction("ChangePassword");
        }

        // --- ROBUST ACTION TO HANDLE THE CONFIRMATION LINK ---
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await _applicationUserHelper.GetUserByIdAsync(userId);
            if (user == null)
            {
                return View("Error");
            }

            var result = await _applicationUserHelper.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                // 1. Sign the user in to create a session.
                await _signInManager.SignInAsync(user, isPersistent: false);

                // 2. CRUCIAL STEP: Refresh the sign-in session. This forces the user's
                // roles and claims to be reloaded into the cookie immediately.
                await _signInManager.RefreshSignInAsync(user);

                // 3. Now that the session is guaranteed to be valid and have the correct roles,
                // check if they have a company.
                var companyExists = await _dataContext.Companies.AnyAsync(c => c.ApplicationUserId == user.Id);
                if (!companyExists)
                {
                    // The redirect will now succeed because the user is properly authenticated WITH their roles.
                    return RedirectToAction("Create", "Companies");
                }

                // If for some reason they already have a company, send them home.
                return RedirectToAction("Index", "Home");
            }

            // If confirmation fails, show an error.
            return View("Error");
        }
    }
}
