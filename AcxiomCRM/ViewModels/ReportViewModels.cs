using AcxiomCRM.Models.Entities;

namespace AcxiomCRM.ViewModels
{
    public class ReportFilterViewModel
    {
        public string ReportType { get; set; } = "Pipeline";
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? Stage { get; set; }
        public string? AssignedTo { get; set; }
        public string? SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 15;
    }

    public class PipelineReportItem
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
        public double PercentageOfPipeline { get; set; }
    }

    public class OwnerPipelineReportItem
    {
        public string OwnerName { get; set; } = string.Empty;
        public int OpportunityCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }

    public class ConversionReportSummary
    {
        public int TotalLeads { get; set; }
        public int ConvertedLeads { get; set; }
        public int LostLeads { get; set; }
        public int ActiveLeads { get; set; }
        public double ConversionRate => TotalLeads > 0 ? Math.Round((double)ConvertedLeads / TotalLeads * 100, 2) : 0;
        public decimal ConvertedOpportunityValue { get; set; }
    }

    public class UserActivityReportItem
    {
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int CallsCount { get; set; }
        public int MeetingsCount { get; set; }
        public int EmailsCount { get; set; }
        public int TasksCount { get; set; }
        public int TotalActivities { get; set; }
        public int FollowUpsCompleted { get; set; }
    }
}
