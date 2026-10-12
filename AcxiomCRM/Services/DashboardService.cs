using AcxiomCRM.Data;
using AcxiomCRM.Models.Entities;
using AcxiomCRM.Models.Enums;
using AcxiomCRM.Services.Interfaces;
using AcxiomCRM.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AcxiomCRM.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly ApplicationDbContext _context;
        private readonly IFollowUpService _followUpService;
        private readonly IActivityService _activityService;

        public DashboardService(ApplicationDbContext context, IFollowUpService followUpService, IActivityService activityService)
        {
            _context = context;
            _followUpService = followUpService;
            _activityService = activityService;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(string userId, IList<string> roles, string userName, string dateFilter = "ThisMonth", DateTime? startDate = null, DateTime? endDate = null)
        {
            var isPrivileged = roles.Contains(SystemRoles.Admin) || roles.Contains(SystemRoles.Manager);
            var now = DateTime.UtcNow;

            // Calculate date bounds
            DateTime? filterStart = null;
            DateTime? filterEnd = null;

            switch (dateFilter.ToLower())
            {
                case "today":
                    filterStart = DateTime.Today;
                    filterEnd = DateTime.Today.AddDays(1).AddTicks(-1);
                    break;
                case "thisweek":
                    var diff = (7 + (DateTime.Today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    filterStart = DateTime.Today.AddDays(-1 * diff);
                    filterEnd = filterStart.Value.AddDays(7).AddTicks(-1);
                    break;
                case "thismonth":
                    filterStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
                    filterEnd = filterStart.Value.AddMonths(1).AddTicks(-1);
                    break;
                case "customrange":
                    if (startDate.HasValue) filterStart = startDate.Value;
                    if (endDate.HasValue) filterEnd = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    break;
                default:
                    dateFilter = "AllTime";
                    break;
            }

            // Customer Queries
            var custQuery = _context.Customers.AsNoTracking().AsQueryable();
            if (!isPrivileged) custQuery = custQuery.Where(c => c.OwnerId == userId);
            if (filterStart.HasValue) custQuery = custQuery.Where(c => c.CreatedDate >= filterStart.Value);
            if (filterEnd.HasValue) custQuery = custQuery.Where(c => c.CreatedDate <= filterEnd.Value);
            var totalCustomers = await custQuery.CountAsync();

            // Lead Queries
            var leadQuery = _context.Leads.AsNoTracking().AsQueryable();
            if (!isPrivileged) leadQuery = leadQuery.Where(l => l.AssignedTo == userId);
            if (filterStart.HasValue) leadQuery = leadQuery.Where(l => l.CreatedDate >= filterStart.Value);
            if (filterEnd.HasValue) leadQuery = leadQuery.Where(l => l.CreatedDate <= filterEnd.Value);

            var totalLeads = await leadQuery.CountAsync();
            var openLeads = await leadQuery.CountAsync(l => l.Status == "New" || l.Status == "Contacted" || l.Status == "Qualified");

            // Opportunity Queries
            var oppQuery = _context.Opportunities.Include(o => o.Customer).AsNoTracking().AsQueryable();
            if (!isPrivileged) oppQuery = oppQuery.Where(o => o.AssignedTo == userId);
            if (filterStart.HasValue) oppQuery = oppQuery.Where(o => o.CreatedDate >= filterStart.Value);
            if (filterEnd.HasValue) oppQuery = oppQuery.Where(o => o.CreatedDate <= filterEnd.Value);

            var totalOpportunities = await oppQuery.CountAsync();
            var openOpps = await oppQuery.Where(o => o.Status == "Open").ToListAsync();
            var openOpportunitiesCount = openOpps.Count;
            var wonOpportunitiesCount = await oppQuery.CountAsync(o => o.Status == "Won");
            var lostOpportunitiesCount = await oppQuery.CountAsync(o => o.Status == "Lost");

            var totalPipelineValue = openOpps.Sum(o => o.Amount);
            var weightedPipelineValue = openOpps.Sum(o => (o.Amount * o.Probability) / 100m);

            // Chart 1: Lead Status counts
            // New, Contacted, Qualified, Lost, Converted
            var allLeadsInScope = _context.Leads.AsNoTracking().AsQueryable();
            if (!isPrivileged) allLeadsInScope = allLeadsInScope.Where(l => l.AssignedTo == userId);
            if (filterStart.HasValue) allLeadsInScope = allLeadsInScope.Where(l => l.CreatedDate >= filterStart.Value);
            if (filterEnd.HasValue) allLeadsInScope = allLeadsInScope.Where(l => l.CreatedDate <= filterEnd.Value);

            var leadStatusGroup = await allLeadsInScope.GroupBy(l => l.Status).Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
            var leadLabels = new List<string> { "New", "Contacted", "Qualified", "Lost", "Converted" };
            var leadCounts = leadLabels.Select(lbl => leadStatusGroup.FirstOrDefault(g => g.Status.Equals(lbl, StringComparison.OrdinalIgnoreCase))?.Count ?? 0).ToList();

            // Chart 2: Opportunity Pipeline Stage counts & amounts
            // Qualification, Proposal, Negotiation, Won, Lost
            var allOppsInScope = _context.Opportunities.AsNoTracking().AsQueryable();
            if (!isPrivileged) allOppsInScope = allOppsInScope.Where(o => o.AssignedTo == userId);
            if (filterStart.HasValue) allOppsInScope = allOppsInScope.Where(o => o.CreatedDate >= filterStart.Value);
            if (filterEnd.HasValue) allOppsInScope = allOppsInScope.Where(o => o.CreatedDate <= filterEnd.Value);

            // Pull to client to avoid SQLite decimal Sum() limitation
            var allOppsRaw = await allOppsInScope.Select(o => new { o.Stage, o.Amount }).ToListAsync();
            var oppStageGroup = allOppsRaw.GroupBy(o => o.Stage).Select(g => new { Stage = g.Key, Count = g.Count(), Total = g.Sum(o => o.Amount) }).ToList();
            var stageLabels = new List<string> { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
            var stageCounts = stageLabels.Select(lbl => oppStageGroup.FirstOrDefault(g => g.Stage.Equals(lbl, StringComparison.OrdinalIgnoreCase))?.Count ?? 0).ToList();
            var stageAmounts = stageLabels.Select(lbl => oppStageGroup.FirstOrDefault(g => g.Stage.Equals(lbl, StringComparison.OrdinalIgnoreCase))?.Total ?? 0m).ToList();

            // Chart 3: Monthly Sales (Last 6 months)
            var monthlyLabels = new List<string>();
            var monthlyValues = new List<decimal>();
            for (int i = 5; i >= 0; i--)
            {
                var monthDate = DateTime.Today.AddMonths(-i);
                var monthStart = new DateTime(monthDate.Year, monthDate.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddTicks(-1);
                var label = monthDate.ToString("MMM yyyy");
                monthlyLabels.Add(label);

                // Pull to client to avoid SQLite decimal Sum() limitation
                var wonList = await _context.Opportunities
                    .Where(o => (isPrivileged || o.AssignedTo == userId) && o.Status == "Won" && o.CreatedDate >= monthStart && o.CreatedDate <= monthEnd)
                    .Select(o => o.Amount)
                    .ToListAsync();
                var wonSum = wonList.Count > 0 ? wonList.Sum() : 0m;

                monthlyValues.Add(wonSum);
            }

            // Recent and upcoming widgets
            var upcomingFollowUps = await _followUpService.GetUpcomingFollowUpsAsync(userId, roles, 5);
            var overdueFollowUps = await _followUpService.GetOverdueFollowUpsAsync(userId, roles, 5);
            var recentActivities = await _activityService.GetRecentActivitiesAsync(userId, roles, 5);

            var highValueOpps = await _context.Opportunities
                .Include(o => o.Customer)
                .Where(o => (isPrivileged || o.AssignedTo == userId) && o.Status == "Open")
                .ToListAsync();

            highValueOpps = highValueOpps
                .OrderByDescending(o => o.Amount)
                .Take(5)
                .ToList();

            return new DashboardViewModel
            {
                UserRole = roles.FirstOrDefault() ?? "Sales Executive",
                UserName = userName,
                DateFilter = dateFilter,
                CustomStartDate = startDate,
                CustomEndDate = endDate,
                TotalCustomers = totalCustomers,
                TotalLeads = totalLeads,
                OpenLeads = openLeads,
                TotalOpportunities = totalOpportunities,
                OpenOpportunities = openOpportunitiesCount,
                WonOpportunities = wonOpportunitiesCount,
                LostOpportunities = lostOpportunitiesCount,
                TotalPipelineValue = totalPipelineValue,
                WeightedPipelineValue = weightedPipelineValue,
                LeadStatusLabels = leadLabels,
                LeadStatusCounts = leadCounts,
                PipelineStageLabels = stageLabels,
                PipelineStageCounts = stageCounts,
                PipelineStageAmounts = stageAmounts,
                MonthlySalesLabels = monthlyLabels,
                MonthlySalesValues = monthlyValues,
                UpcomingFollowUps = upcomingFollowUps,
                OverdueFollowUps = overdueFollowUps,
                RecentActivities = recentActivities,
                HighValueOpportunities = highValueOpps
            };
        }
    }
}
