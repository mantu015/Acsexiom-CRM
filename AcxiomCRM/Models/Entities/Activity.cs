using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AcxiomCRM.Models.Entities
{
    public class Activity
    {
        [Key]
        public int ActivityId { get; set; }

        [Required]
        [MaxLength(30)]
        public string ActivityType { get; set; } = "Call"; // Call, Meeting, Email, Task

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(150)]
        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime ActivityDate { get; set; } = DateTime.UtcNow;

        public int? CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual Customer? Customer { get; set; }

        public int? LeadId { get; set; }
        [ForeignKey("LeadId")]
        public virtual Lead? Lead { get; set; }

        public int? OpportunityId { get; set; }
        [ForeignKey("OpportunityId")]
        public virtual Opportunity? Opportunity { get; set; }

        public string? AssignedTo { get; set; }
        [ForeignKey("AssignedTo")]
        public virtual ApplicationUser? AssignedUser { get; set; }

        [Required]
        [MaxLength(30)]
        public string Status { get; set; } = "Completed"; // Pending, Completed, Cancelled

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}
