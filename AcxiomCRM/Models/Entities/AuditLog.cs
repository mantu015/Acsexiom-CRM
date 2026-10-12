using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public int AuditLogId { get; set; }

        [MaxLength(100)]
        public string? UserId { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [Required]
        [MaxLength(50)]
        public string Action { get; set; } = string.Empty; // Login, Logout, Create, Update, Delete, etc.

        [Required]
        [MaxLength(50)]
        public string EntityName { get; set; } = string.Empty; // Auth, Customer, Lead, Opportunity, etc.

        [MaxLength(50)]
        public string? RecordId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(20)]
        public string Result { get; set; } = "Success"; // Success, Failure

        public string? Details { get; set; }
    }
}
