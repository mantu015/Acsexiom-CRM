using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Lead
    {
        [Key]
        public int LeadId { get; set; }

        [Required]
        [MaxLength(20)]
        public string LeadCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Lead Name is required.")]
        [MaxLength(100, ErrorMessage = "Lead Name cannot exceed 100 characters.")]
        public string LeadName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Enter a valid phone number.")]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? CompanyName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Source { get; set; } = "Website";

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "New";

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Medium";

        [Range(0, 1000000000, ErrorMessage = "Expected Value must be 0 or greater.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal ExpectedValue { get; set; } = 0m;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Assigned Sales Executive
        public string? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public string? Notes { get; set; }

        // Conversion tracking
        public bool IsConverted { get; set; } = false;
        public DateTime? ConvertedDate { get; set; }

        public int? ConvertedCustomerId { get; set; }
        [ForeignKey("ConvertedCustomerId")]
        public virtual Customer? ConvertedCustomer { get; set; }

        public int? ConvertedOpportunityId { get; set; }
        [ForeignKey("ConvertedOpportunityId")]
        public virtual Opportunity? ConvertedOpportunity { get; set; }

        // Navigation Collections
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
