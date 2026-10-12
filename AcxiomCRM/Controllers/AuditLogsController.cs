using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers
{
    [Authorize(Roles = SystemRoles.Admin)]
    public class AuditLogsController : Controller
    {
        private readonly IAuditService _auditService;

        public AuditLogsController(IAuditService auditService)
        {
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? userId, string? entityName, string? action, DateTime? startDate, DateTime? endDate, int page = 1)
        {
            var pagedResult = await _auditService.GetLogsAsync(userId, entityName, action, startDate, endDate, page, 15);

            ViewBag.UserId = userId;
            ViewBag.EntityName = entityName;
            ViewBag.Action = action;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(pagedResult);
        }
    }
}
