using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class FollowUp
    {
        [Key]
        public int FollowUpId { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public virtual Opportunity? Opportunity { get; set; }

        [Required]
        [MaxLength(30)]
        public string RelatedType { get; set; } = "Customer"; // Customer, Lead, Opportunity

        public int RelatedId { get; set; }

        [Required(ErrorMessage = "Follow-up Date is required.")]
        public DateTime FollowUpDate { get; set; }

        [Required]
        [MaxLength(30)]
        public string FollowUpType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(150)]
        public string Subject { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Planned"; // Planned, Completed, Missed, Cancelled

        public string? AssignedTo { get; set; }
        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        public DateTime? CompletionDate { get; set; }
        public string? CompletionRemarks { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
