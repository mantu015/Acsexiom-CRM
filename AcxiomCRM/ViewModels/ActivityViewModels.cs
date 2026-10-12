using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class ActivityFormViewModel
    {
        public int ActivityId { get; set; }

        [Required]
        [Display(Name = "Activity Type")]
        public string ActivityType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(150)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Display(Name = "Description / Summary")]
        public string? Description { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Activity Date")]
        public DateTime ActivityDate { get; set; } = DateTime.Now;

        [Display(Name = "Customer (Optional)")]
        public int? CustomerId { get; set; }

        [Display(Name = "Lead (Optional)")]
        public int? LeadId { get; set; }

        [Display(Name = "Opportunity (Optional)")]
        public int? OpportunityId { get; set; }

        [Display(Name = "Assigned To")]
        public string? AssignedTo { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Completed";
    }
}
