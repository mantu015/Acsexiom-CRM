using AcxiomCRM.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class CustomerFormViewModel
    {
        public int CustomerId { get; set; }

        public string? CustomerCode { get; set; }

        [Required(ErrorMessage = "Customer Name is required.")]
        [MaxLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
        [Display(Name = "Customer Name")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [RegularExpression(@"^(\+?[0-9]{1,4}[\s-]?)?([0-9]{10,12})$", ErrorMessage = "Enter a valid phone number (e.g. 10 digits or with country code).")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        [Display(Name = "Company Name")]
        public string? CompanyName { get; set; }

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [Required]
        public string Status { get; set; } = "Active";

        [Display(Name = "Assigned Sales Executive")]
        public string? OwnerId { get; set; }

        public string? Notes { get; set; }
    }

    public class CustomerDetailsViewModel
    {
        public Customer Customer { get; set; } = null!;
        public List<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public List<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public List<Activity> Activities { get; set; } = new List<Activity>();
    }
}
