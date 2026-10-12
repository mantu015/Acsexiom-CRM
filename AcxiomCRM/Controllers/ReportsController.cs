using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsController(
            IReportService reportService,
            IUserService userService,
            UserManager<ApplicationUser> userManager)
        {
            _reportService = reportService;
            _userService = userService;
            _userManager = userManager;
        }

        private async Task<(string userId, IList<string> roles, string userName)> GetCurrentUserDetailsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            return (user?.Id ?? string.Empty, roles, user?.FullName ?? "Unknown");
        }

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Pipeline));
        }

        [HttpGet]
        public async Task<IActionResult> Pipeline(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var stageData = await _reportService.GetStagePipelineReportAsync(userId, roles, filter);
            var ownerData = await _reportService.GetOwnerPipelineReportAsync(userId, roles, filter);

            ViewBag.StageData = stageData;
            ViewBag.OwnerData = ownerData;
            ViewBag.Filter = filter;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Customers(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var result = await _reportService.GetCustomerReportAsync(userId, roles, filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Leads(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var result = await _reportService.GetLeadReportAsync(userId, roles, filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> FollowUps(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var result = await _reportService.GetFollowUpReportAsync(userId, roles, filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Opportunities(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var result = await _reportService.GetOpportunityReportAsync(userId, roles, filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Conversion(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var result = await _reportService.GetConversionReportAsync(userId, roles, filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> UserActivity(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var result = await _reportService.GetUserActivityReportAsync(userId, roles, filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [HttpGet]
        [Authorize(Roles = SystemRoles.Admin)]
        public async Task<IActionResult> Audit(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var result = await _reportService.GetAuditReportAsync(userId, roles, filter);
            ViewBag.Filter = filter;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCustomersCsv(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            filter.PageSize = 5000;
            var result = await _reportService.GetCustomerReportAsync(userId, roles, filter);

            var headers = new[] { "Customer Code", "Customer Name", "Email", "Phone", "Company", "City", "State", "Status", "Owner", "Created Date" };
            var bytes = _reportService.GenerateCsvExport(result.Items, headers, c => new[]
            {
                c.CustomerCode,
                c.CustomerName,
                c.Email,
                c.Phone,
                c.CompanyName ?? "",
                c.City ?? "",
                c.State ?? "",
                c.Status,
                c.Owner?.FullName ?? "",
                c.CreatedDate.ToString("yyyy-MM-dd HH:mm")
            });

            return File(bytes, "text/csv", $"Customer_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }

        [HttpGet]
        public async Task<IActionResult> ExportPipelineCsv(ReportFilterViewModel filter)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var stageData = await _reportService.GetStagePipelineReportAsync(userId, roles, filter);

            var headers = new[] { "Stage", "Count", "Total Amount ($)", "Weighted Amount ($)", "Pipeline %" };
            var bytes = _reportService.GenerateCsvExport(stageData, headers, s => new[]
            {
                s.Stage,
                s.Count.ToString(),
                s.TotalAmount.ToString("F2"),
                s.WeightedAmount.ToString("F2"),
                $"{s.PercentageOfPipeline}%"
            });

            return File(bytes, "text/csv", $"Pipeline_Report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }
    }
}
