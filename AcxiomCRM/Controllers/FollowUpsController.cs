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
    public class FollowUpsController : Controller
    {
        private readonly IFollowUpService _followUpService;
        private readonly ICustomerService _customerService;
        private readonly ILeadService _leadService;
        private readonly IOpportunityService _opportunityService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsController(
            IFollowUpService followUpService,
            ICustomerService customerService,
            ILeadService leadService,
            IOpportunityService opportunityService,
            IUserService userService,
            UserManager<ApplicationUser> userManager)
        {
            _followUpService = followUpService;
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
            ViewBag.Customers = customers;

            var leads = await _leadService.GetEligibleLeadsForDropdownAsync(userId, roles);
            ViewBag.Leads = leads;

            var opps = await _opportunityService.GetEligibleOpportunitiesForDropdownAsync(userId, roles);
            ViewBag.Opportunities = opps;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm, string? status, string? type, DateTime? date, int page = 1)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var pagedResult = await _followUpService.GetFollowUpsAsync(userId, roles, searchTerm, status, type, date, page, 10);

            ViewBag.SearchTerm = searchTerm;
            ViewBag.Status = status;
            ViewBag.Type = type;
            ViewBag.Date = date?.ToString("yyyy-MM-dd");

            return View(pagedResult);
        }

        [HttpGet]
        public async Task<IActionResult> Create(string? relatedType = "Customer", int? relatedId = null)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();

            var model = new FollowUpFormViewModel
            {
                RelatedType = string.IsNullOrWhiteSpace(relatedType) ? "Customer" : relatedType,
                RelatedId = relatedId ?? 0,
                FollowUpDate = DateTime.Now.AddDays(1).Date.AddHours(10)
            };

            await PopulateDropdownsAsync(userId, roles);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FollowUpFormViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            // Business rule: Follow-up date cannot be earlier than today for a new/planned activity
            if (model.Status == "Planned" && model.FollowUpDate.Date < DateTime.Today)
            {
                ModelState.AddModelError("FollowUpDate", "Follow-up date cannot be earlier than today for a new/planned activity.");
            }

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(userId, roles, model.AssignedTo);
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var followUp = await _followUpService.CreateFollowUpAsync(model, userId, userName, ipAddress);

            TempData["SuccessMessage"] = $"Follow-up '{followUp.Subject}' scheduled successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var followUp = await _followUpService.GetFollowUpByIdAsync(id, userId, roles);

            if (followUp == null)
            {
                TempData["ErrorMessage"] = "Follow-up not found or you are not authorized to edit this record.";
                return RedirectToAction(nameof(Index));
            }

            var model = new FollowUpFormViewModel
            {
                FollowUpId = followUp.FollowUpId,
                RelatedType = followUp.RelatedType,
                RelatedId = followUp.RelatedId,
                FollowUpDate = followUp.FollowUpDate,
                FollowUpType = followUp.FollowUpType,
                Subject = followUp.Subject,
                Remarks = followUp.Remarks,
                Status = followUp.Status,
                AssignedTo = followUp.AssignedTo
            };

            await PopulateDropdownsAsync(userId, roles, model.AssignedTo);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FollowUpFormViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            if (!ModelState.IsValid)
            {
                await PopulateDropdownsAsync(userId, roles, model.AssignedTo);
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var updated = await _followUpService.UpdateFollowUpAsync(model, userId, roles, userName, ipAddress);

            if (!updated)
            {
                TempData["ErrorMessage"] = "Unable to update follow-up or you are not authorized.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = $"Follow-up '{model.Subject}' updated successfully!";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Complete(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var followUp = await _followUpService.GetFollowUpByIdAsync(id, userId, roles);

            if (followUp == null)
            {
                TempData["ErrorMessage"] = "Follow-up not found or you are not authorized.";
                return RedirectToAction(nameof(Index));
            }

            var model = new FollowUpCompleteViewModel
            {
                FollowUpId = followUp.FollowUpId,
                Subject = followUp.Subject,
                RelatedInfo = $"{followUp.RelatedType} #{followUp.RelatedId}",
                FollowUpDate = followUp.FollowUpDate,
                NextFollowUpDate = DateTime.Today.AddDays(7)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Complete(FollowUpCompleteViewModel model)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            if (model.ScheduleNextFollowUp && model.NextFollowUpDate.HasValue && model.NextFollowUpDate.Value.Date < DateTime.Today)
            {
                ModelState.AddModelError("NextFollowUpDate", "Next follow-up date cannot be earlier than today.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var completed = await _followUpService.CompleteFollowUpAsync(model, userId, roles, userName, ipAddress);

            if (!completed)
            {
                TempData["ErrorMessage"] = "Unable to complete follow-up or you are not authorized.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Follow-up marked as Completed and logged to activities!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime newDate, string? remarks)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            if (newDate.Date < DateTime.Today)
            {
                TempData["ErrorMessage"] = "Rescheduled date cannot be in the past.";
                return RedirectToAction(nameof(Index));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var rescheduled = await _followUpService.RescheduleFollowUpAsync(id, newDate, remarks, userId, roles, userName, ipAddress);

            if (!rescheduled)
            {
                TempData["ErrorMessage"] = "Unable to reschedule follow-up or you are not authorized.";
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up rescheduled successfully!";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var deleted = await _followUpService.DeleteFollowUpAsync(id, userId, roles, userName, ipAddress);

            if (!deleted)
            {
                TempData["ErrorMessage"] = "Follow-up not found or you lack permission to delete this record.";
            }
            else
            {
                TempData["SuccessMessage"] = "Follow-up removed successfully.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
