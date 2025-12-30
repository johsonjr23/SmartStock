using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class SaleItemInputViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public string ProductName { get; set; } = "";

        [Required]
        [Range(1, 100000)]
        public int Quantity { get; set; } = 1;

        [Required]
        [Range(0.01, 100000000)]
        public decimal UnitPrice { get; set; }

        // ===== CALCULATED =====
        public decimal SubTotal => Quantity * UnitPrice;

        // Optional (display only)
        public int AvailableStock { get; set; }
    }
}
