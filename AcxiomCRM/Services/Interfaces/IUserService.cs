using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IUserService
    {
        Task<PagedResult<UserItemViewModel>> GetUsersAsync(string? searchTerm, string? role, int page = 1, int pageSize = 10);
        Task<UserEditViewModel?> GetUserForEditAsync(string id);
        Task<(bool Success, string Message)> CreateUserAsync(UserCreateViewModel model, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string Message)> UpdateUserAsync(UserEditViewModel model, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string Message)> ToggleUserActiveStatusAsync(string id, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<(bool Success, string Message)> ResetPasswordAsync(UserResetPasswordViewModel model, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<List<UserItemViewModel>> GetSalesExecutivesForDropdownAsync();
    }
}
