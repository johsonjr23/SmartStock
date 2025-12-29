using System;
using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        // Product affected
        [Required]
        public int ProductId { get; set; }

        // + for StockIn, - for Sale (StockOut)
        [Required]
        public int QuantityChange { get; set; }

        // Purchase, Sale, Adjustment, etc.
        [Required]
        public InventoryTransactionType TransactionType { get; set; }

        // Multi-tenant ownership
        [Required]
        public int TenantId { get; set; }

        // Optional link to sale
        public int? SaleId { get; set; }
        public Sale? Sale { get; set; }

        // System timestamp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Product Product { get; set; } = null!;
    }
}
