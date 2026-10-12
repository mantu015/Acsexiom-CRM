using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = SystemRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(IUserService userService, UserManager<ApplicationUser> userManager)
        {
            _userService = userService;
            _userManager = userManager;
        }

        private async Task<(string userId, string userName)> GetCurrentUserAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            return (user?.Id ?? string.Empty, user?.FullName ?? "Administrator");
        }

        private void PopulateRolesDropdown(string? selectedRole = null)
        {
            ViewBag.Roles = new List<SelectListItem>
            {
                new("-- Select a role --", string.Empty, string.IsNullOrEmpty(selectedRole))
            };
            ((List<SelectListItem>)ViewBag.Roles).AddRange(
                SystemRoles.AllRoles.Select(role => new SelectListItem(role, role, role == selectedRole)));
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? role, int page = 1)
        {
            var pagedResult = await _userService.GetUsersAsync(searchTerm, role, page, 10);
            ViewBag.SearchTerm = searchTerm;
            ViewBag.Role = role;
            PopulateRolesDropdown(role);

            return View(pagedResult);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var model = new UserCreateViewModel();
            PopulateRolesDropdown();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateRolesDropdown(model.Role);
                return View(model);
            }

            var (currentUserId, currentUserName) = await GetCurrentUserAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, message) = await _userService.CreateUserAsync(model, currentUserId, currentUserName, ipAddress);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                PopulateRolesDropdown(model.Role);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var model = await _userService.GetUserForEditAsync(id);
            if (model == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            PopulateRolesDropdown(model.Role);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UserEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                PopulateRolesDropdown(model.Role);
                return View(model);
            }

            var (currentUserId, currentUserName) = await GetCurrentUserAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, message) = await _userService.UpdateUserAsync(model, currentUserId, currentUserName, ipAddress);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                PopulateRolesDropdown(model.Role);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var (currentUserId, currentUserName) = await GetCurrentUserAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, message) = await _userService.ToggleUserActiveStatusAsync(id, currentUserId, currentUserName, ipAddress);

            if (success)
            {
                TempData["SuccessMessage"] = message;
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(Index));
            }

            var model = new UserResetPasswordViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(UserResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var (currentUserId, currentUserName) = await GetCurrentUserAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var (success, message) = await _userService.ResetPasswordAsync(model, currentUserId, currentUserName, ipAddress);

            if (!success)
            {
                ModelState.AddModelError(string.Empty, message);
                return View(model);
            }

            TempData["SuccessMessage"] = message;
            return RedirectToAction(nameof(Index));
        }
    }
}
