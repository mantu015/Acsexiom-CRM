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
    [Route("api/followups")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class FollowUpsApiController : ControllerBase
    {
        private readonly IFollowUpService _followUpService;
        private readonly UserManager<ApplicationUser> _userManager;

        public FollowUpsApiController(IFollowUpService followUpService, UserManager<ApplicationUser> userManager)
        {
            _followUpService = followUpService;
            _userManager = userManager;
        }

        private async Task<(string userId, IList<string> roles, string userName)> GetCurrentUserDetailsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            return (user?.Id ?? string.Empty, roles, user?.FullName ?? "API User");
        }

        [HttpGet]
        public async Task<IActionResult> GetFollowUps([FromQuery] string? search, [FromQuery] string? status, [FromQuery] string? type, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var paged = await _followUpService.GetFollowUpsAsync(userId, roles, search, status, type, null, page, pageSize);

            var dtos = paged.Items.Select(f => new FollowUpDto
            {
                FollowUpId = f.FollowUpId,
                RelatedType = f.RelatedType,
                RelatedId = f.RelatedId,
                FollowUpDate = f.FollowUpDate,
                FollowUpType = f.FollowUpType,
                Subject = f.Subject,
                Remarks = f.Remarks,
                Status = f.Status,
                AssignedUserName = f.AssignedUser?.FullName,
                AssignedTo = f.AssignedTo
            }).ToList();

            return Ok(ApiResponse<List<FollowUpDto>>.Ok(dtos, $"Found {paged.TotalItems} follow-ups."));
        }

        [HttpPost]
        public async Task<IActionResult> CreateFollowUp([FromBody] CreateFollowUpDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid payload.", 400, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            if (dto.Status == "Planned" && dto.FollowUpDate.Date < DateTime.Today)
            {
                return BadRequest(ApiResponse<object>.Fail("Follow-up date cannot be earlier than today for a new/planned activity.", 400));
            }

            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();
            var formModel = new FollowUpFormViewModel
            {
                RelatedType = dto.RelatedType,
                RelatedId = dto.RelatedId,
                FollowUpDate = dto.FollowUpDate,
                FollowUpType = dto.FollowUpType,
                Subject = dto.Subject,
                Remarks = dto.Remarks,
                Status = dto.Status,
                AssignedTo = string.IsNullOrWhiteSpace(dto.AssignedTo) ? userId : dto.AssignedTo
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var followUp = await _followUpService.CreateFollowUpAsync(formModel, userId, userName, ipAddress);

            var createdDto = new FollowUpDto
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

            return StatusCode(201, ApiResponse<FollowUpDto>.Created(createdDto));
        }
    }
}
