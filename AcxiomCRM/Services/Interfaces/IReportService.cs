using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IReportService
    {
        Task<PagedResult<Customer>> GetCustomerReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter);
        Task<PagedResult<Lead>> GetLeadReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter);
        Task<PagedResult<FollowUp>> GetFollowUpReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter);
        Task<PagedResult<Opportunity>> GetOpportunityReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter);
        Task<List<PipelineReportItem>> GetStagePipelineReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter);
        Task<List<OwnerPipelineReportItem>> GetOwnerPipelineReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter);
        Task<ConversionReportSummary> GetConversionReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter);
        Task<List<UserActivityReportItem>> GetUserActivityReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter);
        Task<PagedResult<AuditLog>> GetAuditReportAsync(string userId, IList<string> roles, ReportFilterViewModel filter);
        byte[] GenerateCsvExport<T>(IEnumerable<T> data, string[] headers, Func<T, string[]> rowMapper);
    }
}
