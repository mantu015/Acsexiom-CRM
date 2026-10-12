using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.Services.Interfaces
{
    public interface IOpportunityService
    {
        Task<PagedResult<Opportunity>> GetOpportunitiesAsync(string userId, IList<string> roles, string? searchTerm, string? stage, string? status, int page = 1, int pageSize = 10);
        Task<Opportunity?> GetOpportunityByIdAsync(int id, string userId, IList<string> roles);
        Task<OpportunityDetailsViewModel?> GetOpportunityDetailsAsync(int id, string userId, IList<string> roles);
        Task<string> GenerateOpportunityCodeAsync();
        Task<Opportunity> CreateOpportunityAsync(OpportunityFormViewModel model, string currentUserId, string currentUserName, string? ipAddress = null);
        Task<bool> UpdateOpportunityAsync(OpportunityFormViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<bool> DeleteOpportunityAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<List<Opportunity>> GetEligibleOpportunitiesForDropdownAsync(string userId, IList<string> roles);
    }
}
