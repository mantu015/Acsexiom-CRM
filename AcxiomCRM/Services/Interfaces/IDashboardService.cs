using AcxiomCRM.ViewModels;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(string userId, IList<string> roles, string userName, string dateFilter = "ThisMonth", DateTime? startDate = null, DateTime? endDate = null);
    }
}
