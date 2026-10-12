using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class FollowUpService : IFollowUpService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public FollowUpService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private IQueryable<FollowUp> ApplyScope(IQueryable<FollowUp> query, string userId, IList<string> roles)
        {
            if (roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager))
            {
                return query;
            }

            return query.Where(f => f.AssignedTo == userId);
        }

        public async Task<PagedResult<FollowUp>> GetFollowUpsAsync(string userId, IList<string> roles, string? searchTerm, string? status, string? type, DateTime? date, int page = 1, int pageSize = 10)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsNoTracking();

            query = ApplyScope(query, userId, roles);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(f => f.Subject.ToLower().Contains(term) ||
                                         (f.Customer != null && f.Customer.CustomerName.ToLower().Contains(term)) ||
                                         (f.Lead != null && f.Lead.LeadName.ToLower().Contains(term)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(f => f.Status == status);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(f => f.FollowUpType == type);
            }

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                var nextDay = targetDate.AddDays(1);
                query = query.Where(f => f.FollowUpDate >= targetDate && f.FollowUpDate < nextDay);
            }

            var total = await query.CountAsync();
            var items = await query.OrderBy(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<FollowUp>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = total
            };
        }

        public async Task<FollowUp?> GetFollowUpByIdAsync(int id, string userId, IList<string> roles)
        {
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.Opportunity)
                .Include(f => f.AssignedUser)
                .AsQueryable();

            query = ApplyScope(query, userId, roles);
            return await query.FirstOrDefaultAsync(f => f.FollowUpId == id);
        }

        public async Task<List<FollowUp>> GetUpcomingFollowUpsAsync(string userId, IList<string> roles, int count = 5)
        {
            var now = DateTime.UtcNow;
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .Where(f => f.Status == "Planned" && f.FollowUpDate >= now)
                .AsNoTracking();

            query = ApplyScope(query, userId, roles);
            return await query.OrderBy(f => f.FollowUpDate).Take(count).ToListAsync();
        }

        public async Task<List<FollowUp>> GetOverdueFollowUpsAsync(string userId, IList<string> roles, int count = 5)
        {
            var now = DateTime.UtcNow;
            var query = _context.FollowUps
                .Include(f => f.Customer)
                .Include(f => f.Lead)
                .Include(f => f.AssignedUser)
                .Where(f => f.Status == "Planned" && f.FollowUpDate < now)
                .AsNoTracking();

            query = ApplyScope(query, userId, roles);
            return await query.OrderBy(f => f.FollowUpDate).Take(count).ToListAsync();
        }

        public async Task<FollowUp> CreateFollowUpAsync(FollowUpFormViewModel model, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var followUp = new FollowUp
            {
                RelatedType = model.RelatedType,
                RelatedId = model.RelatedId,
                FollowUpDate = model.FollowUpDate,
                FollowUpType = model.FollowUpType,
                Subject = model.Subject.Trim(),
                Remarks = model.Remarks?.Trim(),
                Status = model.Status,
                AssignedTo = string.IsNullOrWhiteSpace(model.AssignedTo) ? currentUserId : model.AssignedTo,
                CreatedDate = DateTime.UtcNow
            };

            // Associate the appropriate foreign key
            if (model.RelatedType == "Customer")
            {
                followUp.CustomerId = model.RelatedId;
            }
            else if (model.RelatedType == "Lead")
            {
                followUp.LeadId = model.RelatedId;
            }
            else if (model.RelatedType == "Opportunity")
            {
                followUp.OpportunityId = model.RelatedId;
                var opp = await _context.Opportunities.FindAsync(model.RelatedId);
                if (opp != null)
                {
                    followUp.CustomerId = opp.CustomerId;
                }
            }

            _context.FollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                null,
                $"Subject: {followUp.Subject}, Date: {followUp.FollowUpDate:yyyy-MM-dd HH:mm}, Type: {followUp.FollowUpType}",
                $"Scheduled follow-up '{followUp.Subject}'",
                "Success",
                ipAddress);

            return followUp;
        }

        public async Task<bool> UpdateFollowUpAsync(FollowUpFormViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var followUp = await GetFollowUpByIdAsync(model.FollowUpId, userId, roles);
            if (followUp == null) return false;

            var oldValue = $"Date: {followUp.FollowUpDate:yyyy-MM-dd HH:mm}, Status: {followUp.Status}, Type: {followUp.FollowUpType}";

            followUp.FollowUpDate = model.FollowUpDate;
            followUp.FollowUpType = model.FollowUpType;
            followUp.Subject = model.Subject.Trim();
            followUp.Remarks = model.Remarks;
            followUp.Status = model.Status;

            if (roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager))
            {
                followUp.AssignedTo = model.AssignedTo;
            }

            await _context.SaveChangesAsync();

            var newValue = $"Date: {followUp.FollowUpDate:yyyy-MM-dd HH:mm}, Status: {followUp.Status}, Type: {followUp.FollowUpType}";

            await _auditService.LogAsync(
                userId,
                userName,
                "Update",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                oldValue,
                newValue,
                $"Updated follow-up '{followUp.Subject}'",
                "Success",
                ipAddress);

            return true;
        }

        public async Task<bool> CompleteFollowUpAsync(FollowUpCompleteViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var followUp = await GetFollowUpByIdAsync(model.FollowUpId, userId, roles);
            if (followUp == null) return false;

            followUp.Status = "Completed";
            followUp.CompletionDate = DateTime.UtcNow;
            followUp.CompletionRemarks = model.CompletionRemarks.Trim();

            // Also record an Activity for this completed interaction
            var activity = new Activity
            {
                ActivityType = followUp.FollowUpType,
                Subject = $"Completed: {followUp.Subject}",
                Description = model.CompletionRemarks,
                ActivityDate = DateTime.UtcNow,
                CustomerId = followUp.CustomerId,
                LeadId = followUp.LeadId,
                OpportunityId = followUp.OpportunityId,
                AssignedTo = followUp.AssignedTo,
                Status = "Completed",
                CreatedDate = DateTime.UtcNow
            };
            _context.Activities.Add(activity);

            // If user requested scheduling next follow-up
            if (model.ScheduleNextFollowUp && model.NextFollowUpDate.HasValue)
            {
                var nextFollowUp = new FollowUp
                {
                    CustomerId = followUp.CustomerId,
                    LeadId = followUp.LeadId,
                    OpportunityId = followUp.OpportunityId,
                    RelatedType = followUp.RelatedType,
                    RelatedId = followUp.RelatedId,
                    FollowUpDate = model.NextFollowUpDate.Value,
                    FollowUpType = followUp.FollowUpType,
                    Subject = string.IsNullOrWhiteSpace(model.NextFollowUpSubject) ? $"Next follow-up after: {followUp.Subject}" : model.NextFollowUpSubject.Trim(),
                    Remarks = $"Scheduled following completion of follow-up #{followUp.FollowUpId}",
                    Status = "Planned",
                    AssignedTo = followUp.AssignedTo,
                    CreatedDate = DateTime.UtcNow
                };
                _context.FollowUps.Add(nextFollowUp);
            }

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userName,
                "Complete",
                "FollowUp",
                followUp.FollowUpId.ToString(),
                "Status: Planned",
                $"Status: Completed, Remarks: {model.CompletionRemarks}",
                $"Completed follow-up '{followUp.Subject}'",
                "Success",
                ipAddress);

            return true;
        }

        public async Task<bool> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var followUp = await GetFollowUpByIdAsync(id, userId, roles);
            if (followUp == null) return false;

            var oldDateStr = followUp.FollowUpDate.ToString("yyyy-MM-dd HH:mm");
            followUp.FollowUpDate = newDate;
            if (!string.IsNullOrWhiteSpace(remarks))
            {
                followUp.Remarks = (followUp.Remarks ?? "") + $" [Rescheduled from {oldDateStr}: {remarks}]";
            }
            followUp.Status = "Planned";

            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userName,
                "Reschedule",
                "FollowUp",
                id.ToString(),
                oldDateStr,
                newDate.ToString("yyyy-MM-dd HH:mm"),
                $"Rescheduled follow-up '{followUp.Subject}' to {newDate:yyyy-MM-dd HH:mm}",
                "Success",
                ipAddress);

            return true;
        }

        public async Task<bool> DeleteFollowUpAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var followUp = await GetFollowUpByIdAsync(id, userId, roles);
            if (followUp == null) return false;

            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userName,
                "Delete",
                "FollowUp",
                id.ToString(),
                $"Subject: {followUp.Subject}",
                "Deleted",
                $"Deleted follow-up '{followUp.Subject}'",
                "Success",
                ipAddress);

            return true;
        }
    }
}
