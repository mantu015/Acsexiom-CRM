using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }

        [Required]
        [MaxLength(20)]
        public string CustomerCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer Name is required.")]
        [MaxLength(100, ErrorMessage = "Customer Name cannot exceed 100 characters.")]
        public string CustomerName { get; set; } = string.Empty;

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

        [MaxLength(250)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(100)]
        public string CreatedBy { get; set; } = string.Empty;

        // Owner/Sales Executive assignment
        public string? OwnerId { get; set; }

        [ForeignKey("OwnerId")]
        public virtual ApplicationUser? Owner { get; set; }

        public string? Notes { get; set; }

        // Navigation Collections
        public virtual ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
