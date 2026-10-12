using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginDate { get; set; }

        // Navigation properties
        public virtual ICollection<Customer> AssignedCustomers { get; set; } = new List<Customer>();
        public virtual ICollection<Lead> AssignedLeads { get; set; } = new List<Lead>();
        public virtual ICollection<Opportunity> AssignedOpportunities { get; set; } = new List<Opportunity>();
        public virtual ICollection<FollowUp> AssignedFollowUps { get; set; } = new List<FollowUp>();
        public virtual ICollection<Activity> AssignedActivities { get; set; } = new List<Activity>();
    }
}
