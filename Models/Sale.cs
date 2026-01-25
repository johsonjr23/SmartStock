using System;
using System.Collections.Generic;

namespace SmartStock.Models
{
    // Step 5 foundation: sale lifecycle
    public enum SaleStatus
    {
        Draft = 0,
        Completed = 1,
        Voided = 2,
        Refunded = 3
    }

    public partial class Sale
    {
        public int Id { get; set; }

        // Multi-tenant ownership
        public int TenantId { get; set; }

        // Optional invoice / receipt number
        public string? InvoiceNumber { get; set; }

        // Calculated from SaleItems
        public decimal TotalAmount { get; set; }

        // Step 5: lifecycle
        // Default to Completed so existing historical rows remain valid after migration.
        public SaleStatus Status { get; set; } = SaleStatus.Completed;

        public DateTime? CompletedAt { get; set; }

        public DateTime? VoidedAt { get; set; }
        public string? VoidReason { get; set; }
        public int? VoidedBy { get; set; }

        // Optional: future staff/user tracking
        public int? CreatedBy { get; set; }

        // System timestamp
        public DateTime CreatedAt { get; set; }

        public int? OriginalSaleId { get; set; }   // if this Sale is a refund, link to original

        public DateTime? RefundedAt { get; set; }
        public string? RefundReason { get; set; }
        public int? RefundedBy { get; set; }
        public virtual Sale? OriginalSale { get; set; }



        // Navigation
        public virtual ICollection<SaleItem> SaleItems { get; set; }
            = new List<SaleItem>();
    }
}
