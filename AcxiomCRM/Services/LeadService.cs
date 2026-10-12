using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class LeadService : ILeadService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ICustomerService _customerService;

        public LeadService(ApplicationDbContext context, IAuditService auditService, ICustomerService customerService)
        {
            _context = context;
            _auditService = auditService;
            _customerService = customerService;
        }

        private IQueryable<Lead> ApplyScope(IQueryable<Lead> query, string userId, IList<string> roles)
        {
            if (roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager))
            {
                return query;
            }

            return query.Where(l => l.AssignedTo == userId);
        }

        public async Task<PagedResult<Lead>> GetLeadsAsync(string userId, IList<string> roles, string? searchTerm, string? status, string? priority, string? assignedTo, int page = 1, int pageSize = 10)
        {
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .AsNoTracking();

            query = ApplyScope(query, userId, roles);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(l => l.LeadName.ToLower().Contains(term) ||
                                         l.Email.ToLower().Contains(term) ||
                                         l.Phone.Contains(term) ||
                                         (l.CompanyName != null && l.CompanyName.ToLower().Contains(term)) ||
                                         l.LeadCode.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(l => l.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(priority))
            {
                query = query.Where(l => l.Priority == priority);
            }

            if (!string.IsNullOrWhiteSpace(assignedTo))
            {
                query = query.Where(l => l.AssignedTo == assignedTo);
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(l => l.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Lead>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = total
            };
        }

        public async Task<Lead?> GetLeadByIdAsync(int id, string userId, IList<string> roles)
        {
            var query = _context.Leads
                .Include(l => l.AssignedUser)
                .Include(l => l.ConvertedCustomer)
                .Include(l => l.ConvertedOpportunity)
                .AsQueryable();

            query = ApplyScope(query, userId, roles);
            return await query.FirstOrDefaultAsync(l => l.LeadId == id);
        }

        public async Task<LeadDetailsViewModel?> GetLeadDetailsAsync(int id, string userId, IList<string> roles)
        {
            var lead = await GetLeadByIdAsync(id, userId, roles);
            if (lead == null) return null;

            var followUps = await _context.FollowUps
                .Include(f => f.AssignedUser)
                .Where(f => f.LeadId == id || (f.RelatedType == "Lead" && f.RelatedId == id))
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Include(a => a.AssignedUser)
                .Where(a => a.LeadId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            return new LeadDetailsViewModel
            {
                Lead = lead,
                FollowUps = followUps,
                Activities = activities
            };
        }

        public async Task<string> GenerateLeadCodeAsync()
        {
            var maxId = await _context.Leads.MaxAsync(l => (int?)l.LeadId) ?? 0;
            return $"LEAD-{1000 + maxId + 1}";
        }

        public async Task<Lead> CreateLeadAsync(LeadFormViewModel model, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var code = await GenerateLeadCodeAsync();
            var lead = new Lead
            {
                LeadCode = code,
                LeadName = model.LeadName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                CompanyName = model.CompanyName?.Trim(),
                Source = model.Source,
                Status = model.Status,
                Priority = model.Priority,
                ExpectedValue = model.ExpectedValue,
                AssignedTo = string.IsNullOrWhiteSpace(model.AssignedTo) ? currentUserId : model.AssignedTo,
                CreatedDate = DateTime.UtcNow,
                Notes = model.Notes
            };

            _context.Leads.Add(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "Lead",
                lead.LeadId.ToString(),
                null,
                $"Code: {lead.LeadCode}, Name: {lead.LeadName}, Status: {lead.Status}, Value: ${lead.ExpectedValue}",
                $"Created new lead {lead.LeadName} ({lead.LeadCode})",
                "Success",
                ipAddress);

            return lead;
        }

        public async Task<bool> UpdateLeadAsync(LeadFormViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var lead = await GetLeadByIdAsync(model.LeadId, userId, roles);
            if (lead == null) return false;

            var oldValue = $"Name: {lead.LeadName}, Status: {lead.Status}, Priority: {lead.Priority}, Value: {lead.ExpectedValue}, AssignedTo: {lead.AssignedTo}";

            lead.LeadName = model.LeadName.Trim();
            lead.Email = model.Email.Trim();
            lead.Phone = model.Phone.Trim();
            lead.CompanyName = model.CompanyName?.Trim();
            lead.Source = model.Source;
            lead.Status = model.Status;
            lead.Priority = model.Priority;
            lead.ExpectedValue = model.ExpectedValue;
            lead.Notes = model.Notes;

            if (roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager))
            {
                lead.AssignedTo = model.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Name: {lead.LeadName}, Status: {lead.Status}, Priority: {lead.Priority}, Value: {lead.ExpectedValue}, AssignedTo: {lead.AssignedTo}";

            await _auditService.LogAsync(
                userId,
                userName,
                "Update",
                "Lead",
                lead.LeadId.ToString(),
                oldValue,
                newValue,
                $"Updated lead {lead.LeadName}",
                "Success",
                ipAddress);

            return true;
        }

        public async Task<bool> DeleteOrDeactivateLeadAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var lead = await GetLeadByIdAsync(id, userId, roles);
            if (lead == null) return false;

            if (lead.IsConverted)
            {
                // Converted leads should not be deleted to protect CRM history
                lead.Status = "Lost";
                await _context.SaveChangesAsync();
                await _auditService.LogAsync(userId, userName, "Update", "Lead", id.ToString(), "Converted", "Lost", "Marked converted lead as Lost", "Success", ipAddress);
                return true;
            }

            _context.Leads.Remove(lead);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userName,
                "Delete",
                "Lead",
                id.ToString(),
                $"Name: {lead.LeadName}, Code: {lead.LeadCode}",
                "Deleted",
                $"Deleted lead {lead.LeadName}",
                "Success",
                ipAddress);

            return true;
        }

        public async Task<(bool Success, string Message, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(LeadConvertViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var lead = await GetLeadByIdAsync(model.LeadId, userId, roles);
            if (lead == null)
            {
                return (false, "Lead not found or you are not authorized.", null, null);
            }

            if (lead.IsConverted)
            {
                return (false, "Lead has already been converted.", lead.ConvertedCustomerId, lead.ConvertedOpportunityId);
            }

            var isRelational = _context.Database.IsRelational();
            using var transaction = isRelational ? await _context.Database.BeginTransactionAsync() : null;
            try
            {
                // 1. Create or Find Customer
                var existingCustomer = await _context.Customers.FirstOrDefaultAsync(c => c.Email.ToLower() == lead.Email.ToLower() || c.Phone == lead.Phone);
                Customer targetCustomer;

                if (existingCustomer != null)
                {
                    targetCustomer = existingCustomer;
                }
                else
                {
                    var custCode = await _customerService.GenerateCustomerCodeAsync();
                    targetCustomer = new Customer
                    {
                        CustomerCode = custCode,
                        CustomerName = lead.LeadName,
                        Email = lead.Email,
                        Phone = lead.Phone,
                        CompanyName = lead.CompanyName,
                        Status = "Active",
                        OwnerId = lead.AssignedTo ?? userId,
                        CreatedBy = userName,
                        CreatedDate = DateTime.UtcNow,
                        Notes = $"Converted from Lead {lead.LeadCode}. Original notes: {lead.Notes}"
                    };
                    _context.Customers.Add(targetCustomer);
                    await _context.SaveChangesAsync();
                }

                // 2. Optionally create Opportunity
                Opportunity? opp = null;
                if (model.CreateOpportunity)
                {
                    var oppCount = await _context.Opportunities.CountAsync();
                    var oppCode = $"OPP-{1000 + oppCount + 1}";
                    var oppName = string.IsNullOrWhiteSpace(model.OpportunityName) ? $"{lead.LeadName} - Opportunity" : model.OpportunityName.Trim();
                    var oppAmount = (model.OpportunityAmount.HasValue && model.OpportunityAmount.Value > 0) ? model.OpportunityAmount.Value : (lead.ExpectedValue > 0 ? lead.ExpectedValue : 1000m);
                    var closeDate = model.ExpectedCloseDate ?? DateTime.Today.AddDays(30);

                    opp = new Opportunity
                    {
                        OpportunityCode = oppCode,
                        OpportunityName = oppName,
                        CustomerId = targetCustomer.CustomerId,
                        LeadId = lead.LeadId,
                        Amount = oppAmount,
                        Stage = "Qualification",
                        Probability = 25,
                        ExpectedCloseDate = closeDate,
                        Status = "Open",
                        CreatedDate = DateTime.UtcNow,
                        AssignedTo = lead.AssignedTo ?? userId,
                        Notes = $"Auto-created upon converting Lead {lead.LeadCode}"
                    };
                    _context.Opportunities.Add(opp);
                    await _context.SaveChangesAsync();
                }

                // 3. Update Lead status
                lead.Status = "Converted";
                lead.IsConverted = true;
                lead.ConvertedDate = DateTime.UtcNow;
                lead.ConvertedCustomerId = targetCustomer.CustomerId;
                if (opp != null)
                {
                    lead.ConvertedOpportunityId = opp.OpportunityId;
                }
                await _context.SaveChangesAsync();

                // 4. Re-link existing Lead follow-ups and activities to new customer
                var leadFollowUps = await _context.FollowUps.Where(f => f.LeadId == lead.LeadId).ToListAsync();
                foreach (var f in leadFollowUps)
                {
                    f.CustomerId = targetCustomer.CustomerId;
                }

                var leadActivities = await _context.Activities.Where(a => a.LeadId == lead.LeadId).ToListAsync();
                foreach (var a in leadActivities)
                {
                    a.CustomerId = targetCustomer.CustomerId;
                }
                await _context.SaveChangesAsync();

                if (transaction != null) await transaction.CommitAsync();

                await _auditService.LogAsync(
                    userId,
                    userName,
                    "Convert",
                    "Lead",
                    lead.LeadId.ToString(),
                    $"Status: Qualified",
                    $"Converted to CustomerId: {targetCustomer.CustomerId}, OppId: {opp?.OpportunityId}",
                    $"Converted Lead {lead.LeadName} ({lead.LeadCode}) to Customer {targetCustomer.CustomerName}",
                    "Success",
                    ipAddress);

                return (true, "Lead successfully converted!", targetCustomer.CustomerId, opp?.OpportunityId);
            }
            catch (Exception ex)
            {
                if (transaction != null) await transaction.RollbackAsync();
                return (false, $"Error converting lead: {ex.Message}", null, null);
            }
        }

        public async Task<List<Lead>> GetEligibleLeadsForDropdownAsync(string userId, IList<string> roles)
        {
            var query = _context.Leads.Where(l => !l.IsConverted && l.Status != "Lost").AsNoTracking();
            query = ApplyScope(query, userId, roles);
            return await query.OrderBy(l => l.LeadName).ToListAsync();
        }
    }
}
