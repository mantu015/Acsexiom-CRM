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
    public class OpportunitiesController : Controller
    {
        private readonly IOpportunityService _opportunityService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesController(
            IOpportunityService opportunityService,
            ICustomerService customerService,
            ILeadService leadService,
            IUserService userService,
            UserManager<ApplicationUser> userManager)
        {
            _opportunityService = opportunityService;
            _customerService = customerService;
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

        private async Task PopulateDropdownsAsync(string userId, IList<string> roles, int? selectedCustomerId = null, int? selectedLeadId = null, string? selectedAssignee = null)
        {
            var customers = await _customerService.GetAllEligibleCustomersForDropdownAsync(userId, roles);
            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName", selectedCustomerId);

            var leads = await _leadService.GetEligibleLeadsForDropdownAsync(userId, roles);
            ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName", selectedLeadId);

            var reps = await _userService.GetSalesExecutivesForDropdownAsync();
            ViewBag.SalesExecutives = new SelectList(reps, "Id", "FullName", selectedAssignee);
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? stage, string? status, int page = 1)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var pagedResult = await _opportunityService.GetOpportunitiesAsync(userId, roles, searchTerm, stage, status, page, 10);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Stage = stage;
            ViewBag.Status = status;

            return View(pagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var details = await _opportunityService.GetOpportunityDetailsAsync(id, userId, roles);

            if (details == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or you are not authorized to view this record.";
                return RedirectToAction(nameof(Index));
            }

            return View(details);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var model = new OpportunityFormViewModel
            {
                CustomerId = customerId ?? 0,
                LeadId = leadId,
                ExpectedCloseDate = DateTime.Today.AddDays(30)
            };

            await PopulateDropdownsAsync(userId, roles, customerId, leadId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OpportunityFormViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            // Business validations
            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past for an active opportunity.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(userId, roles, model.CustomerId, model.LeadId, model.AssignedTo);
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var opp = await _opportunityService.CreateOpportunityAsync(model, userId, userName, ipAddress);

            TempData["SuccessMessage"] = $"Opportunity '{opp.OpportunityName}' ({opp.OpportunityCode}) created successfully!";
            return RedirectToAction(nameof(Details), new { id = opp.OpportunityId });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, userId, roles);

            if (opp == null)
            {
                TempData["ErrorMessage"] = "Opportunity not found or you are not authorized to edit this record.";
                return RedirectToAction(nameof(Index));
            }

            var model = new OpportunityFormViewModel
            {
                OpportunityId = opp.OpportunityId,
                OpportunityCode = opp.OpportunityCode,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                LeadId = opp.LeadId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                AssignedTo = opp.AssignedTo,
                Notes = opp.Notes
            };

            await PopulateDropdownsAsync(userId, roles, opp.CustomerId, opp.LeadId, opp.AssignedTo);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(OpportunityFormViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            if (model.Amount <= 0)
            {
                ModelState.AddModelError("Amount", "Opportunity Amount must be greater than 0.");
            }

            if (model.Probability < 0 || model.Probability > 100)
            {
                ModelState.AddModelError("Probability", "Probability must be between 0 and 100.");
            }

            if (model.Status == "Open" && model.ExpectedCloseDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("ExpectedCloseDate", "Expected Close Date cannot be in the past for an active opportunity.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(userId, roles, model.CustomerId, model.LeadId, model.AssignedTo);
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var updated = await _opportunityService.UpdateOpportunityAsync(model, userId, roles, userName, ipAddress);

            if (!updated)
            {
                TempData["ErrorMessage"] = "Unable to update opportunity or you are not authorized.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = $"Opportunity '{model.OpportunityName}' updated successfully!";
            return RedirectToAction(nameof(Details), new { id = model.OpportunityId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var deleted = await _opportunityService.DeleteOpportunityAsync(id, userId, roles, userName, ipAddress);

            if (!deleted)
            {
                TempData["ErrorMessage"] = "Opportunity not found or you lack permission to delete this record.";
            }
            else
            {
                TempData["SuccessMessage"] = "Opportunity deleted successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
