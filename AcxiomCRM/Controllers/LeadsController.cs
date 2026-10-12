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
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsController(
            ILeadService leadService,
            IUserService userService,
            UserManager<ApplicationUser> userManager)
        {
            _leadService = leadService;
            _userService = userService;
            _userManager = userManager;
        }

        private async Task<(string userId, IList<string> roles, string userName)> GetCurrentUserDetailsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            return (user?.Id ?? string.Empty, roles, user?.FullName ?? "Unknown");
        }

        private async Task PopulateDropdownsAsync(string? selectedAssignee = null)
        {
            var reps = await _userService.GetSalesExecutivesForDropdownAsync();
            ViewBag.SalesExecutives = new SelectList(reps, "Id", "FullName", selectedAssignee);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? status, string? priority, string? assignedTo, int page = 1)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var pagedResult = await _leadService.GetLeadsAsync(userId, roles, searchTerm, status, priority, assignedTo, page, 10);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.Priority = priority;
            ViewBag.AssignedTo = assignedTo;
            await PopulateDropdownsAsync(assignedTo);

            return View(pagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var details = await _leadService.GetLeadDetailsAsync(id, userId, roles);

            if (details == null)
            {
                TempData["ErrorMessage"] = "Lead not found or you are not authorized to view this record.";
                return RedirectToAction(nameof(Index));
            }

            return View(details);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new LeadFormViewModel();
            await PopulateDropdownsAsync();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadFormViewModel model)
        {
            var (userId, _, userName) = await GetCurrentUserDetailsAsync();

            if (model.ExpectedValue < 0)
            {
                ModelState.AddModelError("ExpectedValue", "Expected value cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.AssignedTo);
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var lead = await _leadService.CreateLeadAsync(model, userId, userName, ipAddress);

            TempData["SuccessMessage"] = $"Lead '{lead.LeadName}' ({lead.LeadCode}) created successfully!";
            return RedirectToAction(nameof(Details), new { id = lead.LeadId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var lead = await _leadService.GetLeadByIdAsync(id, userId, roles);

            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or you are not authorized to edit this record.";
                return RedirectToAction(nameof(Index));
            }

            var model = new LeadFormViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                Source = lead.Source,
                Status = lead.Status,
                Priority = lead.Priority,
                ExpectedValue = lead.ExpectedValue,
                AssignedTo = lead.AssignedTo,
                Notes = lead.Notes
            };

            await PopulateDropdownsAsync(model.AssignedTo);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeadFormViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            if (model.ExpectedValue < 0)
            {
                ModelState.AddModelError("ExpectedValue", "Expected value cannot be negative.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(model.AssignedTo);
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var updated = await _leadService.UpdateLeadAsync(model, userId, roles, userName, ipAddress);

            if (!updated)
            {
                TempData["ErrorMessage"] = "Unable to update lead or you are not authorized.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = $"Lead '{model.LeadName}' updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.LeadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var deleted = await _leadService.DeleteOrDeactivateLeadAsync(id, userId, roles, userName, ipAddress);

            if (!deleted)
            {
                TempData["ErrorMessage"] = "Lead not found or you lack permission to delete this record.";
            }
            else
            {
                TempData["SuccessMessage"] = "Lead record processed successfully.";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Convert(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var lead = await _leadService.GetLeadByIdAsync(id, userId, roles);

            if (lead == null)
            {
                TempData["ErrorMessage"] = "Lead not found or you are not authorized.";
                return RedirectToAction(nameof(Index));
            }

            if (lead.IsConverted)
            {
                TempData["ErrorMessage"] = "This lead has already been converted.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var model = new LeadConvertViewModel
            {
                LeadId = lead.LeadId,
                LeadCode = lead.LeadCode,
                LeadName = lead.LeadName,
                Email = lead.Email,
                Phone = lead.Phone,
                CompanyName = lead.CompanyName,
                CreateOpportunity = true,
                OpportunityName = $"{lead.LeadName} - Project",
                OpportunityAmount = lead.ExpectedValue > 0 ? lead.ExpectedValue : 5000m,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert(LeadConvertViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            if (model.CreateOpportunity)
            {
                if (!model.OpportunityAmount.HasValue || model.OpportunityAmount.Value <= 0)
                {
                    ModelState.AddModelError("OpportunityAmount", "Opportunity amount must be greater than 0.");
                }
                if (model.ExpectedCloseDate.HasValue && model.ExpectedCloseDate.Value.Date < DateTime.Today)
                {
                    ModelState.AddModelError("ExpectedCloseDate", "Expected close date cannot be in the past.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var result = await _leadService.ConvertLeadAsync(model, userId, roles, userName, ipAddress);

            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.Message;
                return View(model);
            }

            TempData["SuccessMessage"] = result.Message;
            if (result.CustomerId.HasValue)
            {
                return RedirectToAction("Details", "Customers", new { id = result.CustomerId.Value });
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
