using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IActivityService
    {
        Task<PagedResult<Activity>> GetActivitiesAsync(string userId, IList<string> roles, string? type, string? status, DateTime? date, int page = 1, int pageSize = 10);
        Task<Activity?> GetActivityByIdAsync(int id, string userId, IList<string> roles);
        Task<List<Activity>> GetRecentActivitiesAsync(string userId, IList<string> roles, int count = 5);
        Task<Activity> CreateActivityAsync(ActivityFormViewModel model, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<bool> DeleteActivityAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null);
    }
}
