using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class ReportsApiController : ControllerBase
    {
        private readonly IReportService _reportService;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportsApiController(IReportService reportService, UserManager<ApplicationUser> userManager)
        {
            _reportService = reportService;
            _userManager = userManager;
        }

        private async Task<(string userId, IList<string> roles)> GetCurrentUserDetailsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            return (user?.Id ?? string.Empty, roles);
        }

        [HttpGet("pipeline")]
        public async Task<IActionResult> GetPipelineReport([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var (userId, roles) = await GetCurrentUserDetailsAsync();
            var filter = new ReportFilterViewModel { StartDate = startDate, EndDate = endDate };

            var stageData = await _reportService.GetStagePipelineReportAsync(userId, roles, filter);

            var totalVal = stageData.Sum(s => s.TotalAmount);
            var weightedVal = stageData.Sum(s => s.WeightedAmount);
            var totalOpps = stageData.Sum(s => s.Count);

            var reportDto = new PipelineReportDto
            {
                TotalPipelineValue = totalVal,
                WeightedPipelineValue = weightedVal,
                TotalOpportunities = totalOpps,
                Stages = stageData.Select(s => new StageSummaryDto
                {
                    Stage = s.Stage,
                    Count = s.Count,
                    TotalAmount = s.TotalAmount,
                    WeightedAmount = s.WeightedAmount
                }).ToList()
            };

            return Ok(ApiResponse<PipelineReportDto>.Ok(reportDto));
        }
    }
}
