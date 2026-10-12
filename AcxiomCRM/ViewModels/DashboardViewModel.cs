using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels
{
    public class DashboardViewModel
    {
        public string UserRole { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string DateFilter { get; set; } = "ThisMonth"; // Today, ThisWeek, ThisMonth, CustomRange
        public DateTime? CustomStartDate { get; set; }
        public DateTime? CustomEndDate { get; set; }

        // KPI Cards
        public int TotalCustomers { get; set; }
        public int TotalLeads { get; set; }
        public int OpenLeads { get; set; }
        public int TotalOpportunities { get; set; }
        public int OpenOpportunities { get; set; }
        public int WonOpportunities { get; set; }
        public int LostOpportunities { get; set; }
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }

        // Chart Data - Lead Status
        public List<string> LeadStatusLabels { get; set; } = new List<string> { "New", "Contacted", "Qualified", "Lost", "Converted" };
        public List<int> LeadStatusCounts { get; set; } = new List<int>();

        // Chart Data - Opportunity Pipeline
        public List<string> PipelineStageLabels { get; set; } = new List<string> { "Qualification", "Proposal", "Negotiation", "Won", "Lost" };
        public List<int> PipelineStageCounts { get; set; } = new List<int>();
        public List<decimal> PipelineStageAmounts { get; set; } = new List<decimal>();

        // Chart Data - Monthly Sales
        public List<string> MonthlySalesLabels { get; set; } = new List<string>();
        public List<decimal> MonthlySalesValues { get; set; } = new List<decimal>();

        // Dynamic CRM Lists
        public List<FollowUp> UpcomingFollowUps { get; set; } = new List<FollowUp>();
        public List<FollowUp> OverdueFollowUps { get; set; } = new List<FollowUp>();
        public List<Activity> RecentActivities { get; set; } = new List<Activity>();
        public List<Opportunity> HighValueOpportunities { get; set; } = new List<Opportunity>();
    }
}
