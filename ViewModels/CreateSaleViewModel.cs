using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class CreateSaleViewModel
    {
        // ===== SALE HEADER =====

        [Required]
        public DateTime SaleDate { get; set; } = DateTime.Today;

        [Required]
        public string PaymentType { get; set; } = "Cash";

        public string? ProductSearch { get; set; }



        // ===== CART ITEMS =====
        public List<SaleItemInputViewModel> Items { get; set; }
            = new List<SaleItemInputViewModel>();

        // ===== CALCULATED (UI ONLY) =====
        public decimal TotalAmount
        {
            get
            {
                decimal total = 0;
                foreach (var item in Items)
                {
                    total += item.SubTotal;
                }
                return total;
            }
        }
    }
}
