using AcxiomCRM.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class FollowUpFormViewModel : IValidatableObject
    {
        public int FollowUpId { get; set; }

        [Required]
        [Display(Name = "Related Module")]
        public string RelatedType { get; set; } = "Customer"; // Customer, Lead, Opportunity

        [Required]
        [Display(Name = "Related Record")]
        public int RelatedId { get; set; }

        [Required(ErrorMessage = "Follow-up Date is required.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Follow-up Date & Time")]
        public DateTime FollowUpDate { get; set; } = DateTime.Now.AddDays(1);

        [Required]
        [Display(Name = "Type")]
        public string FollowUpType { get; set; } = "Call";

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(150)]
        [Display(Name = "Subject")]
        public string Subject { get; set; } = string.Empty;

        [Display(Name = "Remarks / Agenda")]
        public string? Remarks { get; set; }

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Planned";

        [Display(Name = "Assign To")]
        public string? AssignedTo { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (FollowUpId == 0 && Status == "Planned" && FollowUpDate.Date < DateTime.Today)
            {
                yield return new ValidationResult("Follow-up date cannot be earlier than today for a new/planned activity.", new[] { nameof(FollowUpDate) });
            }
        }
    }

    public class FollowUpCompleteViewModel
    {
        public int FollowUpId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string RelatedInfo { get; set; } = string.Empty;
        public DateTime FollowUpDate { get; set; }

        [Required(ErrorMessage = "Completion remarks are required.")]
        [Display(Name = "Completion Remarks / Outcome")]
        public string CompletionRemarks { get; set; } = string.Empty;

        [Display(Name = "Schedule Next Follow-Up?")]
        public bool ScheduleNextFollowUp { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Next Follow-Up Date")]
        public DateTime? NextFollowUpDate { get; set; }

        [Display(Name = "Next Follow-Up Subject")]
        public string? NextFollowUpSubject { get; set; }
    }
}
