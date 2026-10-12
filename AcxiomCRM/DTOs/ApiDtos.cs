using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public int StatusCode { get; set; }

        public static ApiResponse<T> Ok(T data, string message = "Success") =>
            new() { Success = true, Message = message, Data = data, StatusCode = 200 };

        public static ApiResponse<T> Created(T data, string message = "Resource created successfully") =>
            new() { Success = true, Message = message, Data = data, StatusCode = 201 };

        public static ApiResponse<T> Fail(string message, int statusCode = 400, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors, StatusCode = statusCode };
    }

    public class LoginRequestDto
    {
        [Required]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; }
        public string? OwnerName { get; set; }
        public string? OwnerId { get; set; }
    }

    public class CreateCustomerDto
    {
        [Required(ErrorMessage = "Customer Name is required.")]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public string? OwnerId { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateCustomerDto
    {
        [Required(ErrorMessage = "Customer Name is required.")]
        [MaxLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public string? OwnerId { get; set; }
        public string? Notes { get; set; }
    }

    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? AssignedUserName { get; set; }
        public string? AssignedTo { get; set; }
        public bool IsConverted { get; set; }
    }

    public class CreateLeadDto
    {
        [Required(ErrorMessage = "Lead Name is required.")]
        [MaxLength(100)]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        public string Phone { get; set; } = string.Empty;

        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; } = 0;
        public string? AssignedTo { get; set; }
        public string? Notes { get; set; }
    }

    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityCode { get; set; } = string.Empty;
        public string OpportunityName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public decimal WeightedValue { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open";
        public DateTime CreatedDate { get; set; }
        public string? AssignedUserName { get; set; }
        public string? AssignedTo { get; set; }
    }

    public class CreateOpportunityDto
    {
        [Required(ErrorMessage = "Opportunity Name is required.")]
        [MaxLength(150)]
        public string OpportunityName { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        public int? LeadId { get; set; }

        [Required]
        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        public decimal Amount { get; set; }

        public string Stage { get; set; } = "Qualification";

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        [Required]
        public DateTime ExpectedCloseDate { get; set; }

        public string Status { get; set; } = "Open";
        public string? AssignedTo { get; set; }
        public string? Notes { get; set; }
    }

    public class FollowUpDto
    {
        public int FollowUpId { get; set; }
        public string RelatedType { get; set; } = string.Empty;
        public int RelatedId { get; set; }
        public DateTime FollowUpDate { get; set; }
        public string FollowUpType { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string? Remarks { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AssignedUserName { get; set; }
        public string? AssignedTo { get; set; }
    }

    public class CreateFollowUpDto
    {
        [Required]
        public string RelatedType { get; set; } = "Customer";

        [Required]
        public int RelatedId { get; set; }

        [Required]
        public DateTime FollowUpDate { get; set; }

        [Required]
        public string FollowUpType { get; set; } = "Call";

        [Required]
        [MaxLength(150)]
        public string Subject { get; set; } = string.Empty;

        public string? Remarks { get; set; }
        public string Status { get; set; } = "Planned";
        public string? AssignedTo { get; set; }
    }

    public class PipelineReportDto
    {
        public decimal TotalPipelineValue { get; set; }
        public decimal WeightedPipelineValue { get; set; }
        public int TotalOpportunities { get; set; }
        public List<StageSummaryDto> Stages { get; set; } = new List<StageSummaryDto>();
    }

    public class StageSummaryDto
    {
        public string Stage { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal WeightedAmount { get; set; }
    }
}
