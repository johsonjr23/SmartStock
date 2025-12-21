using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class CreateSaleViewModel
    {
        [Required]
        public DateTime SaleDate { get; set; } = DateTime.Today;

        [Required]
        public string PaymentType { get; set; } = "Cash";

        public List<CreateSaleItemViewModel> Items { get; set; } = new();
    }

    public class CreateSaleItemViewModel
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
