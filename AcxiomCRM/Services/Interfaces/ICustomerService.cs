using AcxiomCRM.Models.Entities;
using AcxiomCRM.ViewModels;
using AcxiomCRM.ViewModels.Common;

namespace AcxiomCRM.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<PagedResult<Customer>> GetCustomersAsync(string userId, IList<string> roles, string? searchTerm, string? status, int page = 1, int pageSize = 10);
        Task<Customer?> GetCustomerByIdAsync(int id, string userId, IList<string> roles);
        Task<CustomerDetailsViewModel?> GetCustomerDetailsAsync(int id, string userId, IList<string> roles);
        Task<bool> IsEmailUniqueAsync(string email, int? currentCustomerId = null);
        Task<bool> IsPhoneUniqueAsync(string phone, int? currentCustomerId = null);
        Task<bool> IsDuplicateCustomerAsync(string name, string email, string phone, int? currentCustomerId = null);
        Task<string> GenerateCustomerCodeAsync();
        Task<Customer> CreateCustomerAsync(CustomerFormViewModel model, string createdByUserId, string createdByUserName, string? ipAddress = null);
        Task<bool> UpdateCustomerAsync(CustomerFormViewModel model, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<bool> DeleteOrDeactivateCustomerAsync(int id, string userId, IList<string> roles, string userName, string? ipAddress = null);
        Task<List<Customer>> GetAllEligibleCustomersForDropdownAsync(string userId, IList<string> roles);
    }
}
