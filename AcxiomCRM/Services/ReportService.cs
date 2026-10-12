using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AcxiomCRM.Services
{
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReportService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool IsPrivileged(IList<string> roles) =>
            roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager);

        public async Task<PagedResult<Customer>> GetCustomerReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter)
        {
            var query = _context.Customers.Include(c => c.Owner).AsNoTracking().AsQueryable();
            if (!IsPrivileged(roles)) query = query.Where(c => c.OwnerId == userId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(c => c.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
                query = query.Where(c => c.OwnerId == filter.AssignedTo);

            if (filter.StartDate.HasValue)
                query = query.Where(c => c.CreatedDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(c => c.CreatedDate <= filter.EndDate.Value.Date.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(c => c.CustomerName.ToLower().Contains(term) || c.Email.ToLower().Contains(term) || (c.CompanyName != null && c.CompanyName.ToLower().Contains(term)));
            }

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(c => c.CreatedDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<Customer> { Items = items, PageNumber = filter.PageNumber, PageSize = filter.PageSize, TotalItems = total };
        }

        public async Task<PagedResult<Lead>> GetLeadReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter)
        {
            var query = _context.Leads.Include(l => l.AssignedUser).Include(l => l.ConvertedCustomer).AsNoTracking().AsQueryable();
            if (!IsPrivileged(roles)) query = query.Where(l => l.AssignedTo == userId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(l => l.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
                query = query.Where(l => l.AssignedTo == filter.AssignedTo);

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.CreatedDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(l => l.CreatedDate <= filter.EndDate.Value.Date.AddDays(1).AddTicks(-1));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(l => l.CreatedDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<Lead> { Items = items, PageNumber = filter.PageNumber, PageSize = filter.PageSize, TotalItems = total };
        }

        public async Task<PagedResult<FollowUp>> GetFollowUpReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter)
        {
            var query = _context.FollowUps.Include(f => f.Customer).Include(f => f.Lead).Include(f => f.AssignedUser).AsNoTracking().AsQueryable();
            if (!IsPrivileged(roles)) query = query.Where(f => f.AssignedTo == userId);

            if (!string.IsNullOrWhiteSpace(filter.Status))
            {
                if (filter.Status == "Overdue")
                {
                    var now = DateTime.UtcNow;
                    query = query.Where(f => f.Status == "Planned" && f.FollowUpDate < now);
                }
                else
                {
                    query = query.Where(f => f.Status == filter.Status);
                }
            }

            if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
                query = query.Where(f => f.AssignedTo == filter.AssignedTo);

            if (filter.StartDate.HasValue)
                query = query.Where(f => f.FollowUpDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(f => f.FollowUpDate <= filter.EndDate.Value.Date.AddDays(1).AddTicks(-1));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(f => f.FollowUpDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<FollowUp> { Items = items, PageNumber = filter.PageNumber, PageSize = filter.PageSize, TotalItems = total };
        }

        public async Task<PagedResult<Opportunity>> GetOpportunityReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter)
        {
            var query = _context.Opportunities.Include(o => o.Customer).Include(o => o.AssignedUser).AsNoTracking().AsQueryable();
            if (!IsPrivileged(roles)) query = query.Where(o => o.AssignedTo == userId);

            if (!string.IsNullOrWhiteSpace(filter.Stage))
                query = query.Where(o => o.Stage == filter.Stage);

            if (!string.IsNullOrWhiteSpace(filter.Status))
                query = query.Where(o => o.Status == filter.Status);

            if (!string.IsNullOrWhiteSpace(filter.AssignedTo))
                query = query.Where(o => o.AssignedTo == filter.AssignedTo);

            if (filter.StartDate.HasValue)
                query = query.Where(o => o.CreatedDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(o => o.CreatedDate <= filter.EndDate.Value.Date.AddDays(1).AddTicks(-1));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(o => o.CreatedDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<Opportunity> { Items = items, PageNumber = filter.PageNumber, PageSize = filter.PageSize, TotalItems = total };
        }

        public async Task<List<PipelineReportItem>> GetStagePipelineReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter)
        {
            var query = _context.Opportunities.AsNoTracking().AsQueryable();
            if (!IsPrivileged(roles)) query = query.Where(o => o.AssignedTo == userId);

            if (filter.StartDate.HasValue)
                query = query.Where(o => o.CreatedDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(o => o.CreatedDate <= filter.EndDate.Value.Date.AddDays(1).AddTicks(-1));

            var opps = await query.ToListAsync();
            var totalPipeline = opps.Sum(o => o.Amount);

            var stages = new[] { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var list = new List<PipelineReportItem>();

            foreach (var stg in stages)
            {
                var stageOpps = opps.Where(o => o.Stage.Equals(stg, StringComparison.OrdinalIgnoreCase)).ToList();
                var count = stageOpps.Count;
                var totalAmt = stageOpps.Sum(o => o.Amount);
                var weightedAmt = stageOpps.Sum(o => (o.Amount * o.Probability) / 100m);
                var pct = totalPipeline > 0 ? (double)(totalAmt / totalPipeline) * 100 : 0;

                list.Add(new PipelineReportItem
                {
                    Stage = stg,
                    Count = count,
                    TotalAmount = totalAmt,
                    WeightedAmount = weightedAmt,
                    PercentageOfPipeline = Math.Round(pct, 1)
                });
            }

            return list;
        }

        public async Task<List<OwnerPipelineReportItem>> GetOwnerPipelineReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter)
        {
            var query = _context.Opportunities.Include(o => o.AssignedUser).AsNoTracking().AsQueryable();
            if (!IsPrivileged(roles)) query = query.Where(o => o.AssignedTo == userId);

            if (filter.StartDate.HasValue)
                query = query.Where(o => o.CreatedDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(o => o.CreatedDate <= filter.EndDate.Value.Date.AddDays(1).AddTicks(-1));

            var opps = await query.ToListAsync();

            return opps.GroupBy(o => o.AssignedUser?.FullName ?? "Unassigned")
                .Select(g => new OwnerPipelineReportItem
                {
                    OwnerName = g.Key,
                    OpportunityCount = g.Count(),
                    TotalAmount = g.Sum(o => o.Amount),
                    WeightedAmount = g.Sum(o => (o.Amount * o.Probability) / 100m)
                })
                .OrderByDescending(r => r.TotalAmount)
                .ToList();
        }

        public async Task<ConversionReportSummary> GetConversionReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter)
        {
            var query = _context.Leads.Include(l => l.ConvertedOpportunity).AsNoTracking().AsQueryable();
            if (!IsPrivileged(roles)) query = query.Where(l => l.AssignedTo == userId);

            if (filter.StartDate.HasValue)
                query = query.Where(l => l.CreatedDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(l => l.CreatedDate <= filter.EndDate.Value.Date.AddDays(1).AddTicks(-1));

            var leads = await query.ToListAsync();

            var total = leads.Count;
            var converted = leads.Count(l => l.IsConverted);
            var lost = leads.Count(l => l.Status == "Lost" || l.Status == "Unqualified");
            var active = total - converted - lost;
            var oppVal = leads.Where(l => l.ConvertedOpportunity != null).Sum(l => l.ConvertedOpportunity!.Amount);

            return new ConversionReportSummary
            {
                TotalLeads = total,
                ConvertedLeads = converted,
                LostLeads = lost,
                ActiveLeads = active,
                ConvertedOpportunityValue = oppVal
            };
        }

        public async Task<List<UserActivityReportItem>> GetUserActivityReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter)
        {
            var users = await _userManager.Users.ToListAsync();
            var actQuery = _context.Activities.AsNoTracking().AsQueryable();
            var followUpQuery = _context.FollowUps.Where(f => f.Status == "Completed").AsNoTracking().AsQueryable();

            if (!IsPrivileged(roles))
            {
                users = users.Where(u => u.Id == userId).ToList();
                actQuery = actQuery.Where(a => a.AssignedTo == userId);
                followUpQuery = followUpQuery.Where(f => f.AssignedTo == userId);
            }

            if (filter.StartDate.HasValue)
            {
                actQuery = actQuery.Where(a => a.ActivityDate >= filter.StartDate.Value);
                followUpQuery = followUpQuery.Where(f => f.CompletionDate >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                var end = filter.EndDate.Value.Date.AddDays(1).AddTicks(-1);
                actQuery = actQuery.Where(a => a.ActivityDate <= end);
                followUpQuery = followUpQuery.Where(f => f.CompletionDate <= end);
            }

            var activities = await actQuery.ToListAsync();
            var completedFollowUps = await followUpQuery.ToListAsync();

            var results = new List<UserActivityReportItem>();
            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var userActs = activities.Where(a => a.AssignedTo == user.Id).ToList();
                var userFollowUps = completedFollowUps.Count(f => f.AssignedTo == user.Id);

                results.Add(new UserActivityReportItem
                {
                    UserName = user.FullName,
                    Role = userRoles.FirstOrDefault() ?? "Sales Executive",
                    CallsCount = userActs.Count(a => a.ActivityType == "Call"),
                    MeetingsCount = userActs.Count(a => a.ActivityType == "Meeting"),
                    EmailsCount = userActs.Count(a => a.ActivityType == "Email"),
                    TasksCount = userActs.Count(a => a.ActivityType == "Task"),
                    TotalActivities = userActs.Count,
                    FollowUpsCompleted = userFollowUps
                });
            }

            return results.OrderByDescending(r => r.TotalActivities).ToList();
        }

        public async Task<PagedResult<AuditLog>> GetAuditReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter)
        {
            var query = _context.AuditLogs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.ToLower();
                query = query.Where(a => a.Action.ToLower().Contains(term) || a.EntityName.ToLower().Contains(term) || (a.UserName != null && a.UserName.ToLower().Contains(term)) || (a.Details != null && a.Details.ToLower().Contains(term)));
            }

            if (filter.StartDate.HasValue)
                query = query.Where(a => a.CreatedDate >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(a => a.CreatedDate <= filter.EndDate.Value.Date.AddDays(1).AddTicks(-1));

            var total = await query.CountAsync();
            var items = await query.OrderByDescending(a => a.CreatedDate)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return new PagedResult<AuditLog> { Items = items, PageNumber = filter.PageNumber, PageSize = filter.PageSize, TotalItems = total };
        }

        public byte[] GenerateCsvExport<T>(IEnumerable<T> data, string[] headers, Func<T, string[]> rowMapper)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

            foreach (var item in data)
            {
                var row = rowMapper(item);
                sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string EscapeCsv(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
    }
}
