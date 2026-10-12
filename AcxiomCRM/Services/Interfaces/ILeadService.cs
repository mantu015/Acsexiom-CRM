using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ILeadService
    {
        Task<PagedResult<Lead>> GetLeadsAsync(string userId, IList<string> roles, string? searchTerm, string? status, string? priority, string? assignedTo, int page = 1, int pageSize = 10);
        Task<Lead?> GetLeadByIdAsync(int id, string userId, IList<string> roles);
        Task<LeadDetailsViewModel?> GetLeadDetailsAsync(int id, string userId, IList<string> roles);
        Task<string> GenerateLeadCodeAsync();
        Task<Lead> CreateLeadAsync(LeadFormViewModel model, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<bool> UpdateLeadAsync(LeadFormViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<bool> DeleteOrDeactivateLeadAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<(bool Success, string Message, int? CustomerId, int? OpportunityId)> ConvertLeadAsync(LeadConvertViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<List<Lead>> GetEligibleLeadsForDropdownAsync(string userId, IList<string> roles);
    }
}
