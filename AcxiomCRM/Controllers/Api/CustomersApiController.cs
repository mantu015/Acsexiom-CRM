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
    [Route("api/customers")]
    [Authorize(AuthenticationSchemes = "Bearer,Identity.Application")]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CustomersApiController(ICustomerService customerService, UserManager<ApplicationUser> userManager)
        {
            _customerService = customerService;
            _userManager = userManager;
        }

        private async Task<(string userId, IList<string> roles, string userName)> GetCurrentUserDetailsAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var roles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            return (user?.Id ?? string.Empty, roles, user?.FullName ?? "API User");
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromQuery] string? search, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var paged = await _customerService.GetCustomersAsync(userId, roles, search, status, page, pageSize);

            var dtos = paged.Items.Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                OwnerName = c.Owner?.FullName,
                OwnerId = c.OwnerId
            }).ToList();

            return Ok(ApiResponse<List<CustomerDto>>.Ok(dtos, $"Found {paged.TotalItems} customers."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var (userId, roles, _) = await GetCurrentUserDetailsAsync();
            var customer = await _customerService.GetCustomerByIdAsync(id, userId, roles);

            if (customer == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Customer with ID {id} not found or unauthorized.", 404));
            }

            var dto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                OwnerName = customer.Owner?.FullName,
                OwnerId = customer.OwnerId
            };

            return Ok(ApiResponse<CustomerDto>.Ok(dto));
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid customer payload.", 400, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            if (!await _customerService.IsEmailUniqueAsync(dto.Email))
            {
                return Conflict(ApiResponse<object>.Fail("A customer with this email address already exists.", 409));
            }

            if (!await _customerService.IsPhoneUniqueAsync(dto.Phone))
            {
                return Conflict(ApiResponse<object>.Fail("A customer with this phone number already exists.", 409));
            }

            if (await _customerService.IsDuplicateCustomerAsync(dto.CustomerName, dto.Email, dto.Phone))
            {
                return Conflict(ApiResponse<object>.Fail("Duplicate customer identity detected.", 409));
            }

            var formModel = new CustomerFormViewModel
            {
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = dto.Status ?? "Active",
                OwnerId = string.IsNullOrWhiteSpace(dto.OwnerId) ? userId : dto.OwnerId,
                Notes = dto.Notes
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var customer = await _customerService.CreateCustomerAsync(formModel, userId, userName, ipAddress);

            var createdDto = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.CustomerName,
                Email = customer.Email,
                Phone = customer.Phone,
                CompanyName = customer.CompanyName,
                Address = customer.Address,
                City = customer.City,
                State = customer.State,
                Status = customer.Status,
                CreatedDate = customer.CreatedDate,
                OwnerId = customer.OwnerId
            };

            return StatusCode(201, ApiResponse<CustomerDto>.Created(createdDto));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.Fail("Invalid payload.", 400, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList()));
            }

            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();

            var existing = await _customerService.GetCustomerByIdAsync(id, userId, roles);
            if (existing == null)
            {
                return NotFound(ApiResponse<object>.Fail($"Customer with ID {id} not found or unauthorized.", 404));
            }

            if (!await _customerService.IsEmailUniqueAsync(dto.Email, id))
            {
                return Conflict(ApiResponse<object>.Fail("Email is already taken by another customer.", 409));
            }

            if (!await _customerService.IsPhoneUniqueAsync(dto.Phone, id))
            {
                return Conflict(ApiResponse<object>.Fail("Phone is already taken by another customer.", 409));
            }

            var formModel = new CustomerFormViewModel
            {
                CustomerId = id,
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = dto.Status,
                OwnerId = dto.OwnerId,
                Notes = dto.Notes
            };

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var updated = await _customerService.UpdateCustomerAsync(formModel, userId, roles, userName, ipAddress);

            if (!updated)
            {
                return Forbid();
            }

            return Ok(ApiResponse<object>.Ok(new { id }, "Customer updated successfully."));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var (userId, roles, userName) = await GetCurrentUserDetailsAsync();
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            var deleted = await _customerService.DeleteOrDeactivateCustomerAsync(id, userId, roles, userName, ipAddress);
            if (!deleted)
            {
                return NotFound(ApiResponse<object>.Fail($"Customer with ID {id} not found or unauthorized.", 404));
            }

            return Ok(ApiResponse<object>.Ok(new { id }, "Customer deleted or deactivated successfully."));
        }
    }
}
