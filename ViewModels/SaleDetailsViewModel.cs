using System;
using System.Collections.Generic;

namespace SmartStock.ViewModels
{
    public class SaleDetailsViewModel
    {
        public int Id { get; set; }
        public string DisplayInvoiceNumber { get; set; } = "";
        public DateTime SaleDate { get; set; }
        public string PaymentType { get; set; } = "";
        public decimal TotalAmount { get; set; }

        // ===== STATUS / VOID =====
        public string Status { get; set; } = "";   // Completed, Voided, Refunded
        public DateTime? VoidedAt { get; set; }
        public string? VoidReason { get; set; }

        // ===== REFUND =====
        public int? OriginalSaleId { get; set; }   // If this is a refund
        public DateTime? RefundedAt { get; set; }
        public string? RefundReason { get; set; }

        public List<SaleDetailsItemRow> Items { get; set; } = new();

        public class SaleDetailsItemRow
        {
            public int ProductId { get; set; }
            public string ProductName { get; set; } = "";
            public string? SKU { get; set; }
            public decimal Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal LineTotal => Quantity * UnitPrice;
        }
    }
}
