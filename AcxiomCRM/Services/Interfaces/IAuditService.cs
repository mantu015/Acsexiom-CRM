using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IAuditService
    {
        Task LogAsync(string? userId, string? userName, string action, string entityName, string? recordId, string? oldValue, string? newValue, string? details, string result = "Success", string? ipAddress = null);
        Task<PagedResult<AuditLog>> GetLogsAsync(string? userId, string? entityName, string? action, DateTime? startDate, DateTime? endDate, int page = 1, int pageSize = 20);
    }
}
