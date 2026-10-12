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
    [Route("api/opportunities")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OpportunitiesApiController(IOpportunityService opportunityService, UserManager<ApplicationUser> userManager)
        {
            _opportunityService = opportunityService;
            _userManager = userManager;
        }

        private async Task<(string userId, IList<string> roles, string userName)> GetCurrentUserDetailsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            return (user?.Id ?? string.Empty, roles, user?.FullName ?? "API User");
        }

        [HttpGet]
        public async Task<IActionResult> GetOpportunities([FromQuery] string? search, [FromQuery] string? stage, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var paged = await _opportunityService.GetOpportunitiesAsync(userId, roles, search, stage, status, page, pageSize);

            var dtos = paged.Items.Select(o => new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityCode = o.OpportunityCode,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName ?? string.Empty,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                WeightedValue = o.WeightedValue,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                CreatedDate = o.CreatedDate,
                AssignedUserName = o.AssignedUser?.FullName,
                AssignedTo = o.AssignedTo
            }).ToList();

            return Ok(ApiResponse<List<OpportunityDto>>.Ok(dtos, $"Found {paged.TotalItems} opportunities."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOpportunity(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var opp = await _opportunityService.GetOpportunityByIdAsync(id, userId, roles);

            if (opp == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Opportunity with ID {id} not found or unauthorized.", 404));
            }

            var dto = new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityCode = opp.OpportunityCode,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                CustomerName = opp.Customer?.CustomerName ?? string.Empty,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                WeightedValue = opp.WeightedValue,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                CreatedDate = opp.CreatedDate,
                AssignedUserName = opp.AssignedUser?.FullName,
                AssignedTo = opp.AssignedTo
            };

            return Ok(ApiResponse<OpportunityDto>.Ok(dto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid payload.", 400, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            if (dto.Amount <= 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Opportunity Amount must be greater than 0.", 400));
            }

            if (dto.Probability < 0 || dto.Probability > 100)
            {
                return BadRequest(ApiResponse<object>.Fail("Probability must be between 0 and 100.", 400));
            }

            if (dto.Status == "Open" && dto.ExpectedCloseDate.Date < DateTime.Today)
            {
                return BadRequest(ApiResponse<object>.Fail("Expected Close Date cannot be in the past for an active opportunity.", 400));
            }

            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();
            var formModel = new OpportunityFormViewModel
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = dto.Status,
                AssignedTo = string.IsNullOrWhiteSpace(dto.AssignedTo) ? userId : dto.AssignedTo,
                Notes = dto.Notes
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var opp = await _opportunityService.CreateOpportunityAsync(formModel, userId, userName, ipAddress);

            var createdDto = new OpportunityDto
            {
                OpportunityId = opp.OpportunityId,
                OpportunityCode = opp.OpportunityCode,
                OpportunityName = opp.OpportunityName,
                CustomerId = opp.CustomerId,
                Amount = opp.Amount,
                Stage = opp.Stage,
                Probability = opp.Probability,
                WeightedValue = opp.WeightedValue,
                ExpectedCloseDate = opp.ExpectedCloseDate,
                Status = opp.Status,
                CreatedDate = opp.CreatedDate,
                AssignedTo = opp.AssignedTo
            };

            return StatusCode(201, ApiResponse<OpportunityDto>.Created(createdDto));
        }
    }
}
