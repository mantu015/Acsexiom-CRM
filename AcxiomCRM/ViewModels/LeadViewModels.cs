using AcxiomCRM.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class LeadFormViewModel
    {
        public int LeadId { get; set; }

        public string? LeadCode { get; set; }

        [Required(ErrorMessage = "Lead Name is required.")]
        [MaxLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
        [Display(Name = "Lead Name")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^(\+?[0-9]{1,4}[\s-]?)?([0-9]{10,12})$", ErrorMessage = "Enter a valid phone number.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [Required]
        [Display(Name = "Lead Source")]
        public string Source { get; set; } = "Website";

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "New";

        [Required]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Medium";

        [Required(ErrorMessage = "Expected Value is required.")]
        [Range(0, 1000000000, ErrorMessage = "Expected Value must be 0 or greater.")]
        [Display(Name = "Expected Value ($)")]
        public decimal ExpectedValue { get; set; } = 0m;

        [Display(Name = "Assign To Sales Executive")]
        public string? AssignedTo { get; set; }

        public string? Notes { get; set; }
    }

    public class LeadDetailsViewModel
    {
        public Lead Lead { get; set; } = null!;
        public List<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public List<Activity> Activities { get; set; } = new List<Activity>();
    }

    public class LeadConvertViewModel
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }

        [Display(Name = "Create Sales Opportunity?")]
        public bool CreateOpportunity { get; set; } = true;

        [Display(Name = "Opportunity Name")]
        public string? OpportunityName { get; set; }

        [Display(Name = "Opportunity Amount ($)")]
        public decimal? OpportunityAmount { get; set; }

        [Display(Name = "Expected Close Date")]
        public DateTime? ExpectedCloseDate { get; set; }
    }
}
