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
    public class ActivitiesController : Controller
    {
        private readonly IActivityService _activityService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly IOpportunityService _opportunityService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ActivitiesController(
            IActivityService activityService,
            ICustomerService customerService,
            ILeadService leadService,
            IOpportunityService opportunityService,
            IUserService userService,
            UserManager<ApplicationUser> userManager)
        {
            _activityService = activityService;
            _customerService = customerService;
            _leadService = leadService;
            _opportunityService = opportunityService;
            _userService = userService;
            _userManager = userManager;
        }

        private async Task<(string userId, IList<string> roles, string userName)> GetCurrentUserDetailsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            return (user?.Id ?? string.Empty, roles, user?.FullName ?? "Unknown");
        }

        private async Task PopulateDropdownsAsync(string userId, IList<string> roles, string? selectedAssignee = null)
        {
            var reps = await _userService.GetSalesExecutivesForDropdownAsync();
            ViewBag.SalesExecutives = new SelectList(reps, "Id", "FullName", selectedAssignee);

            var customers = await _customerService.GetAllEligibleCustomersForDropdownAsync(userId, roles);
            ViewBag.Customers = new SelectList(customers, "CustomerId", "CustomerName");

            var leads = await _leadService.GetEligibleLeadsForDropdownAsync(userId, roles);
            ViewBag.Leads = new SelectList(leads, "LeadId", "LeadName");

            var opps = await _opportunityService.GetEligibleOpportunitiesForDropdownAsync(userId, roles);
            ViewBag.Opportunities = new SelectList(opps, "OpportunityId", "OpportunityName");
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? type, string? status, DateTime? date, int page = 1)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var pagedResult = await _activityService.GetActivitiesAsync(userId, roles, type, status, date, page, 10);

            ViewBag.Type = type;
            ViewBag.Status = status;
            ViewBag.Date = date?.ToString("yyyy-MM-dd");

            return View(pagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? customerId = null, int? leadId = null, int? opportunityId = null)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var model = new ActivityFormViewModel
            {
                CustomerId = customerId,
                LeadId = leadId,
                OpportunityId = opportunityId,
                ActivityDate = DateTime.Now
            };

            await PopulateDropdownsAsync(userId, roles);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ActivityFormViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(userId, roles, model.AssignedTo);
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var activity = await _activityService.CreateActivityAsync(model, userId, userName, ipAddress);

            TempData["SuccessMessage"] = $"Activity '{activity.Subject}' recorded successfully!";

            if (model.CustomerId.HasValue)
            {
                return RedirectToAction("Details", "Customers", new { id = model.CustomerId.Value });
            }
            if (model.LeadId.HasValue)
            {
                return RedirectToAction("Details", "Leads", new { id = model.LeadId.Value });
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var deleted = await _activityService.DeleteActivityAsync(id, userId, roles, userName, ipAddress);

            if (!deleted)
            {
                TempData["ErrorMessage"] = "Activity not found or you lack permission to delete this record.";
            }
            else
            {
                TempData["SuccessMessage"] = "Activity removed successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
