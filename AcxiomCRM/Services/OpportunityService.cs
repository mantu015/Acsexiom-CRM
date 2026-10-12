using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public OpportunityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private IQueryable<Opportunity> ApplyScope(IQueryable<Opportunity> query, string userId, IList<string> roles)
        {
            if (roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager))
            {
                return query;
            }

            return query.Where(o => o.AssignedTo == userId);
        }

        public async Task<PagedResult<Opportunity>> GetOpportunitiesAsync(string userId, IList<string> roles, string? searchTerm, string? stage, string? status, int page = 1, int pageSize = 10)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsNoTracking();

            query = ApplyScope(query, userId, roles);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(o => o.OpportunityName.ToLower().Contains(term) ||
                                         o.OpportunityCode.ToLower().Contains(term) ||
                                         o.Customer!.CustomerName.ToLower().Contains(term));
            }

            if (!string.IsNullOrWhiteSpace(stage))
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(o => o.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Opportunity>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = total
            };
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int id, string userId, IList<string> roles)
        {
            var query = _context.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.Lead)
                .Include(o => o.AssignedUser)
                .AsQueryable();

            query = ApplyScope(query, userId, roles);
            return await query.FirstOrDefaultAsync(o => o.OpportunityId == id);
        }

        public async Task<OpportunityDetailsViewModel?> GetOpportunityDetailsAsync(int id, string userId, IList<string> roles)
        {
            var opp = await GetOpportunityByIdAsync(id, userId, roles);
            if (opp == null) return null;

            var followUps = await _context.FollowUps
                .Include(f => f.AssignedUser)
                .Where(f => f.OpportunityId == id || (f.RelatedType == "Opportunity" && f.RelatedId == id))
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();

            var activities = await _context.Activities
                .Include(a => a.AssignedUser)
                .Where(a => a.OpportunityId == id)
                .OrderByDescending(a => a.ActivityDate)
                .ToListAsync();

            return new OpportunityDetailsViewModel
            {
                Opportunity = opp,
                FollowUps = followUps,
                Activities = activities
            };
        }

        public async Task<string> GenerateOpportunityCodeAsync()
        {
            var maxId = await _context.Opportunities.MaxAsync(o => (int?)o.OpportunityId) ?? 0;
            return $"OPP-{1000 + maxId + 1}";
        }

        public async Task<Opportunity> CreateOpportunityAsync(OpportunityFormViewModel model, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var code = await GenerateOpportunityCodeAsync();

            // Auto-align status with stage
            var status = model.Stage == "Won" ? "Won" : (model.Stage == "Lost" ? "Lost" : "Open");

            var opp = new Opportunity
            {
                OpportunityCode = code,
                OpportunityName = model.OpportunityName.Trim(),
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                Amount = model.Amount,
                Stage = model.Stage,
                Probability = model.Probability,
                ExpectedCloseDate = model.ExpectedCloseDate,
                Status = status,
                AssignedTo = string.IsNullOrWhiteSpace(model.AssignedTo) ? currentUserId : model.AssignedTo,
                CreatedDate = DateTime.UtcNow,
                Notes = model.Notes
            };

            _context.Opportunities.Add(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "Opportunity",
                opp.OpportunityId.ToString(),
                null,
                $"Code: {opp.OpportunityCode}, Name: {opp.OpportunityName}, Amount: ${opp.Amount}, Stage: {opp.Stage}, Prob: {opp.Probability}%",
                $"Created new opportunity {opp.OpportunityName} ({opp.OpportunityCode})",
                "Success",
                ipAddress);

            return opp;
        }

        public async Task<bool> UpdateOpportunityAsync(OpportunityFormViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var opp = await GetOpportunityByIdAsync(model.OpportunityId, userId, roles);
            if (opp == null) return false;

            var oldValue = $"Name: {opp.OpportunityName}, Amount: {opp.Amount}, Stage: {opp.Stage}, Prob: {opp.Probability}, Status: {opp.Status}, Close: {opp.ExpectedCloseDate:yyyy-MM-dd}";

            // Auto-align status with stage
            var status = model.Stage == "Won" ? "Won" : (model.Stage == "Lost" ? "Lost" : model.Status);

            opp.OpportunityName = model.OpportunityName.Trim();
            opp.CustomerId = model.CustomerId;
            opp.LeadId = model.LeadId;
            opp.Amount = model.Amount;
            opp.Stage = model.Stage;
            opp.Probability = model.Probability;
            opp.ExpectedCloseDate = model.ExpectedCloseDate;
            opp.Status = status;
            opp.Notes = model.Notes;

            if (roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager))
            {
                opp.AssignedTo = model.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Name: {opp.OpportunityName}, Amount: {opp.Amount}, Stage: {opp.Stage}, Prob: {opp.Probability}, Status: {opp.Status}, Close: {opp.ExpectedCloseDate:yyyy-MM-dd}";

            await _auditService.LogAsync(
                userId,
                userName,
                "Update",
                "Opportunity",
                opp.OpportunityId.ToString(),
                oldValue,
                newValue,
                $"Updated opportunity {opp.OpportunityName}",
                "Success",
                ipAddress);

            return true;
        }

        public async Task<bool> DeleteOpportunityAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var opp = await GetOpportunityByIdAsync(id, userId, roles);
            if (opp == null) return false;

            _context.Opportunities.Remove(opp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userName,
                "Delete",
                "Opportunity",
                id.ToString(),
                $"Name: {opp.OpportunityName}, Code: {opp.OpportunityCode}",
                "Deleted",
                $"Deleted opportunity {opp.OpportunityName}",
                "Success",
                ipAddress);

            return true;
        }

        public async Task<List<Opportunity>> GetEligibleOpportunitiesForDropdownAsync(string userId, IList<string> roles)
        {
            var query = _context.Opportunities.Where(o => o.Status == "Open").AsNoTracking();
            query = ApplyScope(query, userId, roles);
            return await query.OrderBy(o => o.OpportunityName).ToListAsync();
        }
    }
}
