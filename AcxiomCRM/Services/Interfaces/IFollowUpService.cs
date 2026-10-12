using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IFollowUpService
    {
        Task<PagedResult<FollowUp>> GetFollowUpsAsync(string userId, IList<string> roles, string? searchTerm, string? status, string? type, DateTime? date, int page = 1, int pageSize = 10);
        Task<FollowUp?> GetFollowUpByIdAsync(int id, string userId, IList<string> roles);
        Task<List<FollowUp>> GetUpcomingFollowUpsAsync(string userId, IList<string> roles, int count = 5);
        Task<List<FollowUp>> GetOverdueFollowUpsAsync(string userId, IList<string> roles, int count = 5);
        Task<FollowUp> CreateFollowUpAsync(FollowUpFormViewModel model, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<bool> UpdateFollowUpAsync(FollowUpFormViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<bool> CompleteFollowUpAsync(FollowUpCompleteViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<bool> RescheduleFollowUpAsync(int id, DateTime newDate, string? remarks, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<bool> DeleteFollowUpAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null);
    }
}
