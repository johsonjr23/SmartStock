using System;
using System.Collections.Generic;

namespace SmartStock.Models
{
    public partial class Sale
    {
        public int Id { get; set; }

        // Multi-tenant ownership
        public int TenantId { get; set; }

        // Optional invoice / receipt number
        public string? InvoiceNumber { get; set; }

        // Calculated from SaleItems
        public decimal TotalAmount { get; set; }

        // Cash, MobileMoney, Card (keep string for now)
        public string PaymentType { get; set; } = "Cash";

        // Date sale happened (business date)
        public DateTime SaleDate { get; set; }

        // Optional: future staff/user tracking
        public int? CreatedBy { get; set; }

        // System timestamp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual ICollection<SaleItem> SaleItems { get; set; }
            = new List<SaleItem>();
    }
}
