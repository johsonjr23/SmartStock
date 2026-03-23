using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class CreateTenantViewModel
    {
        [Required(ErrorMessage = "Business name is required")]
        [Display(Name = "Business Name")]
        [MaxLength(200)]
        public string BusinessName { get; set; } = string.Empty;

        [Display(Name = "Owner Name")]
        [MaxLength(200)]
        public string? OwnerName { get; set; }

        [Display(Name = "Phone")]
        [MaxLength(50)]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Admin email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Admin Email")]
        public string AdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        [Display(Name = "Password")]
        public string AdminPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please confirm the password")]
        [DataType(DataType.Password)]
        [Compare("AdminPassword", ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string AdminConfirmPassword { get; set; } = string.Empty;
    }
}
