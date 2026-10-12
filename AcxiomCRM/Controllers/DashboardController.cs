using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(IDashboardService dashboardService, UserManager<ApplicationUser> userManager)
        {
            _dashboardService = dashboardService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string dateFilter = "ThisMonth", DateTime? startDate = null, DateTime? endDate = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var roles = await _userManager.GetRolesAsync(user);
            var viewModel = await _dashboardService.GetDashboardDataAsync(user.Id, roles, user.FullName, dateFilter, startDate, endDate);

            return View(viewModel);
        }
    }
}
