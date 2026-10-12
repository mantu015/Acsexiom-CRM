using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Opportunity
    {
        [Key]
        public int OpportunityId { get; set; }

        [Required]
        [MaxLength(20)]
        public string OpportunityCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [MaxLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer is required.")]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }

        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(30)]
        public string Stage { get; set; } = "Qualification";

        [Required(ErrorMessage = "Probability is required.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        public DateTime ExpectedCloseDate { get; set; }

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "Open"; // Open, Won, Lost

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public string? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public string? Notes { get; set; }

        [NotMapped]
        public decimal WeightedValue => (Amount * Probability) / 100m;

        // Navigation Collections
        public virtual ICollection<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> Activities { get; set; } = new List<Activity>();
    }
}
