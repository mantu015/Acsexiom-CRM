using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public CustomerService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private IQueryable<Customer> ApplyScope(IQueryable<Customer> query, string userId, IList<string> roles)
        {
            if (roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager))
            {
                return query;
            }

            // Sales Executive can only access their assigned customers
            return query.Where(c => c.OwnerId == userId);
        }

        public async Task<PagedResult<Customer>> GetCustomersAsync(string userId, IList<string> roles, string? searchTerm, string? status, int page = 1, int pageSize = 10)
        {
            var query = _context.Customers
                .Include(c => c.Owner)
                .AsNoTracking();

            query = ApplyScope(query, userId, roles);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term) ||
                                         c.Email.ToLower().Contains(term) ||
                                         c.Phone.Contains(term) ||
                                         (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)) ||
                                         c.CustomerCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(c => c.Status == status);
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(c => c.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Customer>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = total
            };
        }

        public async Task<Customer?> GetCustomerByIdAsync(int id, string userId, IList<string> roles)
        {
            var query = _context.Customers.Include(c => c.Owner).AsQueryable();
            query = ApplyScope(query, userId, roles);
            return await query.FirstOrDefaultAsync(c => c.CustomerId == id);
        }

        public async Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int id, string userId, IList<string> roles)
        {
            var customer = await GetCustomerByIdAsync(id, userId, roles);
            if (customer == null) return null;

            var opportunities = await _context.Opportunities
                .Include(o => o.AssignedUser)
                .Where(o => o.CustomerId == id)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            var followUps = await _context.FollowUps
                .Include(f => f.AssignedUser)
                .Where(f => f.CustomerId == id || (f.RelatedType == "Customer" && f.RelatedId == id))
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Include(a => a.AssignedUser)
                .Where(a => a.CustomerId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            return new CustomerDetailsViewModel
            {
                Customer = customer,
                Opportunities = opportunities,
                FollowUps = followUps,
                Activities = activities
            };
        }

        public async Task<bool> IsEmailUniqueAsync(string email, int? currentCustomerId = null)
        {
            var normalized = email.Trim().ToLower();
            return !await _context.Customers.AnyAsync(c => c.Email.ToLower() == normalized && (!currentCustomerId.HasValue || c.CustomerId != currentCustomerId.Value));
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, int? currentCustomerId = null)
        {
            var cleanPhone = phone.Trim();
            return !await _context.Customers.AnyAsync(c => c.Phone == cleanPhone && (!currentCustomerId.HasValue || c.CustomerId != currentCustomerId.Value));
        }

        public async Task<bool> IsDuplicateCustomerAsync(string name, string email, string phone, int? currentCustomerId = null)
        {
            var normName = name.Trim().ToLower();
            var normEmail = email.Trim().ToLower();
            var cleanPhone = phone.Trim();

            return await _context.Customers.AnyAsync(c =>
                (!currentCustomerId.HasValue || c.CustomerId != currentCustomerId.Value) &&
                (c.Email.ToLower() == normEmail || c.Phone == cleanPhone || c.CustomerName.ToLower() == normName));
        }

        public async Task<string> GenerateCustomerCodeAsync()
        {
            var maxId = await _context.Customers.MaxAsync(c => (int?)c.CustomerId) ?? 0;
            return $"CUST-{1000 + maxId + 1}";
        }

        public async Task<Customer> CreateCustomerAsync(CustomerFormViewModel model, string createdByUserId, string createdByUserName, string? ipAddress = null)
        {
            var code = await GenerateCustomerCodeAsync();
            var customer = new Customer
            {
                CustomerCode = code,
                CustomerName = model.CustomerName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Address = model.Address?.Trim(),
                City = model.City?.Trim(),
                State = model.State?.Trim(),
                Status = model.Status,
                OwnerId = string.IsNullOrWhiteSpace(model.OwnerId) ? createdByUserId : model.OwnerId,
                CreatedBy = createdByUserName,
                CreatedDate = DateTime.UtcNow,
                Notes = model.Notes
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                createdByUserId,
                createdByUserName,
                "Create",
                "Customer",
                customer.CustomerId.ToString(),
                null,
                $"Code: {customer.CustomerCode}, Name: {customer.CustomerName}, Email: {customer.Email}",
                $"Created new customer {customer.CustomerName} ({customer.CustomerCode})",
                "Success",
                ipAddress);

            return customer;
        }

        public async Task<bool> UpdateCustomerAsync(CustomerFormViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var customer = await GetCustomerByIdAsync(model.CustomerId, userId, roles);
            if (customer == null) return false;

            var oldValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Phone: {customer.Phone}, Status: {customer.Status}, Owner: {customer.OwnerId}";

            customer.CustomerName = model.CustomerName.Trim();
            customer.Email = model.Email.Trim();
            customer.Phone = model.Phone.Trim();
            customer.CompanyName = model.CompanyName?.Trim();
            customer.Address = model.Address?.Trim();
            customer.City = model.City?.Trim();
            customer.State = model.State?.Trim();
            customer.Status = model.Status;
            customer.ModifiedDate = DateTime.UtcNow;
            customer.Notes = model.Notes;

            if (roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager))
            {
                customer.OwnerId = model.OwnerId;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Name: {customer.CustomerName}, Email: {customer.Email}, Phone: {customer.Phone}, Status: {customer.Status}, Owner: {customer.OwnerId}";

            await _auditService.LogAsync(
                userId,
                userName,
                "Update",
                "Customer",
                customer.CustomerId.ToString(),
                oldValue,
                newValue,
                $"Updated customer {customer.CustomerName}",
                "Success",
                ipAddress);

            return true;
        }

        public async Task<bool> DeleteOrDeactivateCustomerAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var customer = await GetCustomerByIdAsync(id, userId, roles);
            if (customer == null) return false;

            var hasOpportunities = await _context.Opportunities.AnyAsync(o => o.CustomerId == id);
            var hasFollowUps = await _context.FollowUps.AnyAsync(f => f.CustomerId == id || (f.RelatedType == "Customer" && f.RelatedId == id));

            string actionTaken;
            if (hasOpportunities || hasFollowUps)
            {
                // Soft deactivate to protect relational integrity and business history
                customer.Status = "Inactive";
                customer.ModifiedDate = DateTime.UtcNow;
                actionTaken = "Deactivated (Preserved history)";
            }
            else
            {
                _context.Customers.Remove(customer);
                actionTaken = "Deleted";
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userName,
                "Delete",
                "Customer",
                id.ToString(),
                $"Status: {customer.Status}",
                actionTaken,
                $"{actionTaken} customer {customer.CustomerName}",
                "Success",
                ipAddress);

            return true;
        }

        public async Task<List<Customer>> GetAllEligibleCustomersForDropdownAsync(string userId, IList<string> roles)
        {
            var query = _context.Customers.Where(c => c.Status != "Inactive").AsNoTracking();
            query = ApplyScope(query, userId, roles);
            return await query.OrderBy(c => c.CustomerName).ToListAsync();
        }
    }
}
