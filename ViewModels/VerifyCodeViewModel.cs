using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class VerifyCodeViewModel
    {
        [Required(ErrorMessage = "Please enter the code")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Code must be 6 digits")]
        public string Code { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
