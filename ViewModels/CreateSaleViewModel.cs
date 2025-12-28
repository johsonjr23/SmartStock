using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class CreateSaleViewModel
    {
        [Required]
        [DataType(DataType.Date)]
        public DateTime SaleDate { get; set; } = DateTime.Today;

        [Required]
        public string PaymentType { get; set; } = "Cash";

        [Required]
        public List<CreateSaleItemViewModel> Items { get; set; }
            = new List<CreateSaleItemViewModel>();
    }

    public class CreateSaleItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Selling price must be greater than zero")]
        public decimal SellingPrice { get; set; }
    }
}
