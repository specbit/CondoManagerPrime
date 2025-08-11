using CET96_ProjetoFinal.web.Data;
using CET96_ProjetoFinal.web.Entities;
using CET96_ProjetoFinal.web.Helpers;
using CET96_ProjetoFinal.web.Models;
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

        public AccountController(
            IApplicationUserHelper userHelper, 
            ApplicationUserDataContext context,
            SignInManager<ApplicationUser> signInManager)
        {
            _applicationUserHelper = userHelper;
            _dataContext = context;
            _signInManager = signInManager;
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
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _applicationUserHelper.LoginAsync(model);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // GET: /Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
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
                        UserName = model.Username, // Identity requires UserName
                        Email = model.Username,
                        IdentificationDocument = model.IdentificationDocument,
                        DocumentType = model.DocumentType,
                        PhoneNumber = model.PhoneNumber
                    };

                    // Create the user in the database
                    var result = await _applicationUserHelper.AddUserAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        await _applicationUserHelper.AddUserToRoleAsync(user, "Company Administrator");

                        // Create the associated Company 
                        var company = new Company
                        {
                            Name = model.CompanyName,
                            ApplicationUserId = user.Id, // Link the company to the new user
                            UserCreatedId = user.Id,
                            PaymentValidated = true // Simulate a successful payment
                        };

                        _dataContext.Companies.Add(company);
                        await _dataContext.SaveChangesAsync();

                        // Instead of using the helper for this one action, take direct control 
                        // of the sign-in process to ensure the session is updated immediately.
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        // This is the crucial line that forces the login cookie to be recognized.
                        await _signInManager.RefreshSignInAsync(user);

                        // Send them to the home page
                        return RedirectToAction("Index", "Home");
                    }

                    // If creation failed, add errors to the page
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "A user with this email already exists.");
                }
            }

            // If we got this far, something failed, redisplay form
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
    }
}
