using System.ComponentModel.DataAnnotations;

namespace AcxiomCRM.ViewModels
{
    public class UserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsLockedOut { get; set; }
        public int FailedLoginCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(50)]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
    }

    public class UserEditViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Full Name is required.")]
        [MaxLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Role is required.")]
        public string Role { get; set; } = string.Empty;

        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }

        [Display(Name = "Unlock Account (if currently locked)")]
        public bool UnlockAccount { get; set; }
    }

    public class UserResetPasswordViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "New Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
