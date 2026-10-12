using AcxiomCRM.DTOs;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AcxiomCRM.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthApiController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly IAuditService _auditService;

        public AuthApiController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IConfiguration configuration,
            IAuditService auditService)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _configuration = configuration;
            _auditService = auditService;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid login payload.", 400, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var user = await _userManager.FindByEmailAsync(dto.EmailOrUsername)
                       ?? await _userManager.FindByNameAsync(dto.EmailOrUsername);

            if (user == null || !user.IsActive)
            {
                await _auditService.LogAsync(null, dto.EmailOrUsername, "Failed Login API", "Auth", null, null, null, "API invalid user or deactivated", "Failure", ipAddress);
                return Unauthorized(ApiResponse<object>.Fail("Invalid credentials or account is inactive.", 401));
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, dto.Password, lockoutOnFailure: true);

            if (result.IsLockedOut)
            {
                await _auditService.LogAsync(user.Id, user.UserName, "Lockout API", "Auth", user.Id, null, null, "Account locked out via API attempt", "Failure", ipAddress);
                return StatusCode(423, ApiResponse<object>.Fail("Account is locked out due to repeated failed attempts.", 423));
            }

            if (!result.Succeeded)
            {
                await _auditService.LogAsync(user.Id, user.UserName, "Failed Login API", "Auth", user.Id, null, null, "API incorrect password", "Failure", ipAddress);
                return Unauthorized(ApiResponse<object>.Fail("Invalid credentials.", 401));
            }

            var roles = await _userManager.GetRolesAsync(user);
            var primaryRole = roles.FirstOrDefault() ?? "Sales Executive";

            // Generate JWT Token
            var secret = _configuration["JwtSettings:Secret"]
                ?? throw new InvalidOperationException("JWT signing secret is not configured.");
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, primaryRole)
            };

            var expiration = DateTime.UtcNow.AddHours(24);
            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"] ?? "AcxiomCRM",
                audience: _configuration["JwtSettings:Audience"] ?? "AcxiomCRMClients",
                claims: claims,
                expires: expiration,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            await _auditService.LogAsync(user.Id, user.UserName, "Login API", "Auth", user.Id, null, null, "API Token generated", "Success", ipAddress);

            var responseData = new LoginResponseDto
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = primaryRole,
                Token = tokenString,
                Expiration = expiration
            };

            return Ok(ApiResponse<LoginResponseDto>.Ok(responseData, "Login successful"));
        }

        [HttpPost("logout")]
        [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.GetUserAsync(User);
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (user != null)
            {
                await _auditService.LogAsync(user.Id, user.UserName, "Logout API", "Auth", user.Id, null, null, "API Session terminated", "Success", ipAddress);
            }

            await _signInManager.SignOutAsync();
            return Ok(ApiResponse<object>.Ok(new { }, "Logged out successfully"));
        }
    }
}
