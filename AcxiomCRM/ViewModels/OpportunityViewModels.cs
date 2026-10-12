using AcxiomCRM.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class OpportunityFormViewModel : IValidatableObject
    {
        public int OpportunityId { get; set; }

        public string? OpportunityCode { get; set; }

        [Required(ErrorMessage = "Opportunity Name is required.")]
        [MaxLength(150, ErrorMessage = "Opportunity Name cannot exceed 150 characters.")]
        [Display(Name = "Opportunity Name")]
        public string OpportunityName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Customer selection is required.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Display(Name = "Associated Lead (Optional)")]
        public int? LeadId { get; set; }

        [Required(ErrorMessage = "Opportunity Amount is required.")]
        [Range(0.01, 1000000000, ErrorMessage = "Opportunity Amount must be greater than 0.")]
        [Display(Name = "Amount ($)")]
        public decimal Amount { get; set; }

        [Required]
        [Display(Name = "Stage")]
        public string Stage { get; set; } = "Qualification";

        [Required(ErrorMessage = "Probability is required.")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        [Display(Name = "Probability (%)")]
        public int Probability { get; set; } = 20;

        [Required(ErrorMessage = "Expected Close Date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Expected Close Date")]
        public DateTime ExpectedCloseDate { get; set; } = DateTime.Today.AddDays(30);

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Open";

        [Display(Name = "Assigned Sales Executive")]
        public string? AssignedTo { get; set; }

        public string? Notes { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Amount <= 0)
            {
                yield return new ValidationResult("Opportunity Amount must be greater than 0.", new[] { nameof(Amount) });
            }

            if (Probability < 0 || Probability > 100)
            {
                yield return new ValidationResult("Probability must be between 0 and 100.", new[] { nameof(Probability) });
            }

            if (Status == "Open" && ExpectedCloseDate.Date < DateTime.Today)
            {
                yield return new ValidationResult("Expected Close Date cannot be in the past for an active opportunity.", new[] { nameof(ExpectedCloseDate) });
            }
        }
    }

    public class OpportunityDetailsViewModel
    {
        public Opportunity Opportunity { get; set; } = null!;
        public List<FollowUp> FollowUps { get; set; } = new List<FollowUp>();
        public List<Activity> Activities { get; set; } = new List<Activity>();
    }
}
