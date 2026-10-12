using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class CustomersController : Controller
    {
        private readonly ICustomerService _customerService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersController(
            ICustomerService customerService,
            IUserService userService,
            UserManager<ApplicationUser> userManager)
        {
            _customerService = customerService;
            _userService = userService;
            _userManager = userManager;
        }

        private async Task<(string userId, IList<string> roles, string userName)> GetCurrentUserDetailsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            return (user?.Id ?? string.Empty, roles, user?.FullName ?? "Unknown");
        }

        private async Task PopulateSalesExecutivesDropdownAsync(string? selectedId = null)
        {
            var reps = await _userService.GetSalesExecutivesForDropdownAsync();
            ViewBag.SalesExecutives = new SelectList(reps, "Id", "FullName", selectedId);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? status, int page = 1)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var pagedResult = await _customerService.GetCustomersAsync(userId, roles, searchTerm, status, page, 10);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;

            return View(pagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var details = await _customerService.GetCustomerDetailsAsync(id, userId, roles);

            if (details == null)
            {
                TempData["ErrorMessage"] = "Customer not found or you are not authorized to view this record.";
                return RedirectToAction(nameof(Index));
            }

            return View(details);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new CustomerFormViewModel();
            await PopulateSalesExecutivesDropdownAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerFormViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            // Server-side uniqueness & duplicate validations
            if (!await _customerService.IsEmailUniqueAsync(model.Email))
            {
                ModelState.AddModelError("Email", "A customer with this email address already exists.");
            }

            if (!await _customerService.IsPhoneUniqueAsync(model.Phone))
            {
                ModelState.AddModelError("Phone", "A customer with this phone number already exists.");
            }

            if (await _customerService.IsDuplicateCustomerAsync(model.CustomerName, model.Email, model.Phone))
            {
                ModelState.AddModelError(string.Empty, "A duplicate customer with the same business identity already exists.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSalesExecutivesDropdownAsync(model.OwnerId);
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var customer = await _customerService.CreateCustomerAsync(model, userId, userName, ipAddress);

            TempData["SuccessMessage"] = $"Customer '{customer.CustomerName}' ({customer.CustomerCode}) created successfully!";
            return RedirectToAction(nameof(Details), new { id = customer.CustomerId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var customer = await _customerService.GetCustomerByIdAsync(id, userId, roles);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found or you are not authorized to edit this record.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CustomerFormViewModel
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                OwnerId = customer.OwnerId,
                Notes = customer.Notes
            };

            await PopulateSalesExecutivesDropdownAsync(model.OwnerId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CustomerFormViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            if (!await _customerService.IsEmailUniqueAsync(model.Email, model.CustomerId))
            {
                ModelState.AddModelError("Email", "Another customer with this email address already exists.");
            }

            if (!await _customerService.IsPhoneUniqueAsync(model.Phone, model.CustomerId))
            {
                ModelState.AddModelError("Phone", "Another customer with this phone number already exists.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateSalesExecutivesDropdownAsync(model.OwnerId);
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var updated = await _customerService.UpdateCustomerAsync(model, userId, roles, userName, ipAddress);

            if (!updated)
            {
                TempData["ErrorMessage"] = "Unable to update customer or you are not authorized.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = $"Customer '{model.CustomerName}' updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.CustomerId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var deleted = await _customerService.DeleteOrDeactivateCustomerAsync(id, userId, roles, userName, ipAddress);

            if (!deleted)
            {
                TempData["ErrorMessage"] = "Customer not found or you lack permission to delete this record.";
            }
            else
            {
                TempData["SuccessMessage"] = "Customer has been safely removed or deactivated.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
