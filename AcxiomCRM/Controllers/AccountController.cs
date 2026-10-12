using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _auditService = auditService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Find user by username or email
            var user = await _userManager.FindByEmailAsync(model.EmailOrUsername)
                       ?? await _userManager.FindByNameAsync(model.EmailOrUsername);

            if (user == null)
            {
                await _auditService.LogAsync(null, model.EmailOrUsername, "Failed Login", "Auth", null, null, null, $"Unknown user attempt: {model.EmailOrUsername}", "Failure", ipAddress);
                ModelState.AddModelError(string.Empty, "Invalid login attempt. Please check your credentials.");
                return View(model);
            }

            if (!user.IsActive)
            {
                await _auditService.LogAsync(user.Id, user.UserName, "Failed Login", "Auth", user.Id, null, null, "Account is deactivated", "Failure", ipAddress);
                ModelState.AddModelError(string.Empty, "Your account has been deactivated. Please contact an administrator.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName!, model.Password, model.RememberMe, lockoutOnFailure: true);

            if (result.Succeeded)
            {
                user.LastLoginDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                await _auditService.LogAsync(user.Id, user.UserName, "Login", "Auth", user.Id, null, null, "User logged in successfully", "Success", ipAddress);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
                return RedirectToAction("Index", "Dashboard");
            }

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(user.Id, user.UserName, "Lockout", "Auth", user.Id, null, null, "User account locked due to multiple failed login attempts", "Failure", ipAddress);
                ModelState.AddModelError(string.Empty, "This account has been temporarily locked out due to multiple failed login attempts. Please try again later.");
                return View(model);
            }

            await _auditService.LogAsync(user.Id, user.UserName, "Failed Login", "Auth", user.Id, null, null, "Incorrect password provided", "Failure", ipAddress);
            ModelState.AddModelError(string.Empty, "Invalid login credentials. Please try again.");
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "An account with this email address already exists.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName.Trim(),
                IsActive = true,
                EmailConfirmed = true,
                CreatedDate = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var administrators = await _userManager.GetUsersInRoleAsync(SystemRoles.Admin);
                var assignedRole = administrators.Count == 0
                    ? SystemRoles.Admin
                    : SystemRoles.SalesExecutive;

                await _userManager.AddToRoleAsync(user, assignedRole);

                await _auditService.LogAsync(user.Id, user.UserName, "Create", "User", user.Id, null, $"Role: {assignedRole}", $"Self-registered as {assignedRole}", "Success", ipAddress);

                TempData["SuccessMessage"] = "Your account has been created. Please sign in to continue.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (user != null)
            {
                await _auditService.LogAsync(user.Id, user.UserName, "Logout", "Auth", user.Id, null, null, "User logged out", "Success", ipAddress);
            }

            await _signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                await _signInManager.RefreshSignInAsync(user);
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                await _auditService.LogAsync(user.Id, user.UserName, "Security", "User", user.Id, null, "Password Changed", "User changed their password", "Success", ipAddress);
                TempData["SuccessMessage"] = "Your password has been changed successfully!";
                return RedirectToAction("Index", "Dashboard");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }
    }
}
