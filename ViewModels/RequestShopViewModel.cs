using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class RequestShopViewModel
    {
        [Required(ErrorMessage = "Shop name is required")]
        [Display(Name = "Shop / Business Name")]
        public string ShopName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Your name is required")]
        [Display(Name = "Owner / Contact Name")]
        public string OwnerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Business Type")]
        public string? BusinessType { get; set; }

        [Display(Name = "Additional Message")]
        [MaxLength(1000)]
        public string? Message { get; set; }
    }
}
