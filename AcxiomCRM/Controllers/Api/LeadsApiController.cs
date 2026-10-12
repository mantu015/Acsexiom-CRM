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
    [Route("api/leads")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class LeadsApiController : ControllerBase
    {
        private readonly ILeadService _leadService;
        private readonly UserManager<ApplicationUser> _userManager;

        public LeadsApiController(ILeadService leadService, UserManager<ApplicationUser> userManager)
        {
            _leadService = leadService;
            _userManager = userManager;
        }

        private async Task<(string userId, IList<string> roles, string userName)> GetCurrentUserDetailsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            return (user?.Id ?? string.Empty, roles, user?.FullName ?? "API User");
        }

        [HttpGet]
        public async Task<IActionResult> GetLeads([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? priority, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var paged = await _leadService.GetLeadsAsync(userId, roles, search, status, priority, null, page, pageSize);

            var dtos = paged.Items.Select(l => new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                AssignedUserName = l.AssignedUser?.FullName,
                AssignedTo = l.AssignedTo,
                IsConverted = l.IsConverted
            }).ToList();

            return Ok(ApiResponse<List<LeadDto>>.Ok(dtos, $"Found {paged.TotalItems} leads."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLead(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var lead = await _leadService.GetLeadByIdAsync(id, userId, roles);

            if (lead == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Lead with ID {id} not found or unauthorized.", 404));
            }

            var dto = new LeadDto
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
                CreatedDate = lead.CreatedDate,
                AssignedUserName = lead.AssignedUser?.FullName,
                AssignedTo = lead.AssignedTo,
                IsConverted = lead.IsConverted
            };

            return Ok(ApiResponse<LeadDto>.Ok(dto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid payload.", 400, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            if (dto.ExpectedValue < 0)
            {
                return BadRequest(ApiResponse<object>.Fail("Expected value cannot be negative.", 400));
            }

            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();
            var formModel = new LeadFormViewModel
            {
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                Status = dto.Status,
                Priority = dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                AssignedTo = string.IsNullOrWhiteSpace(dto.AssignedTo) ? userId : dto.AssignedTo,
                Notes = dto.Notes
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var lead = await _leadService.CreateLeadAsync(formModel, userId, userName, ipAddress);

            var createdDto = new LeadDto
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
                CreatedDate = lead.CreatedDate,
                AssignedTo = lead.AssignedTo,
                IsConverted = lead.IsConverted
            };

            return StatusCode(201, ApiResponse<LeadDto>.Created(createdDto));
        }
    }
}
