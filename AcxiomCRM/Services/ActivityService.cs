using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public ActivityService(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        private IQueryable<Activity> ApplyScope(IQueryable<Activity> query, string userId, IList<string> roles)
        {
            if (roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager))
            {
                return query;
            }

            return query.Where(a => a.AssignedTo == userId);
        }

        public async Task<PagedResult<Activity>> GetActivitiesAsync(string userId, IList<string> roles, string? type, string? status, DateTime? date, int page = 1, int pageSize = 10)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedUser)
                .AsNoTracking();

            query = ApplyScope(query, userId, roles);

            if (!string.IsNullOrWhiteSpace(type))
            {
                query = query.Where(a => a.ActivityType == type);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(a => a.Status == status);
            }

            if (date.HasValue)
            {
                var targetDate = date.Value.Date;
                var nextDay = targetDate.AddDays(1);
                query = query.Where(a => a.ActivityDate >= targetDate && a.ActivityDate < nextDay);
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.ActivityDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedResult<Activity>
            {
                Items = items,
                PageNumber = page,
                PageSize = pageSize,
                TotalItems = total
            };
        }

        public async Task<Activity?> GetActivityByIdAsync(int id, string userId, IList<string> roles)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.Opportunity)
                .Include(a => a.AssignedUser)
                .AsQueryable();

            query = ApplyScope(query, userId, roles);
            return await query.FirstOrDefaultAsync(a => a.ActivityId == id);
        }

        public async Task<List<Activity>> GetRecentActivitiesAsync(string userId, IList<string> roles, int count = 5)
        {
            var query = _context.Activities
                .Include(a => a.Customer)
                .Include(a => a.Lead)
                .Include(a => a.AssignedUser)
                .AsNoTracking();

            query = ApplyScope(query, userId, roles);
            return await query.OrderByDescending(a => a.ActivityDate).Take(count).ToListAsync();
        }

        public async Task<Activity> CreateActivityAsync(ActivityFormViewModel model, string currentUserId, string currentUserName, string? ipAddress = null)
        {
            var activity = new Activity
            {
                ActivityType = model.ActivityType,
                Subject = model.Subject.Trim(),
                Description = model.Description?.Trim(),
                ActivityDate = model.ActivityDate,
                CustomerId = model.CustomerId,
                LeadId = model.LeadId,
                OpportunityId = model.OpportunityId,
                AssignedTo = string.IsNullOrWhiteSpace(model.AssignedTo) ? currentUserId : model.AssignedTo,
                Status = model.Status,
                CreatedDate = DateTime.UtcNow
            };

            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                currentUserId,
                currentUserName,
                "Create",
                "Activity",
                activity.ActivityId.ToString(),
                null,
                $"Type: {activity.ActivityType}, Subject: {activity.Subject}",
                $"Logged activity '{activity.Subject}'",
                "Success",
                ipAddress);

            return activity;
        }

        public async Task<bool> DeleteActivityAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null)
        {
            var activity = await GetActivityByIdAsync(id, userId, roles);
            if (activity == null) return false;

            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            await _auditService.LogAsync(
                userId,
                userName,
                "Delete",
                "Activity",
                id.ToString(),
                $"Subject: {activity.Subject}",
                "Deleted",
                $"Deleted activity '{activity.Subject}'",
                "Success",
                ipAddress);

            return true;
        }
    }
}
