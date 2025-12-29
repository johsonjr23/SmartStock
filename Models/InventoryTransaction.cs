using System;
using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        // =========================
        // RELATIONSHIPS
        // =========================

        [Required]
        public int ProductId { get; set; }

        public Product Product { get; set; } = null!;

        // Multi-tenant ownership
        [Required]
        public int TenantId { get; set; }

        // Optional link to Sale (only for Sale transactions)
        public int? SaleId { get; set; }
        public Sale? Sale { get; set; }

        // =========================
        // TRANSACTION DATA
        // =========================

        /// <summary>
        /// Positive = Stock In / Positive Adjustment
        /// Negative = Sale / Negative Adjustment
        /// </summary>
        [Required]
        public int QuantityChange { get; set; }

        [Required]
        public InventoryTransactionType TransactionType { get; set; }

        /// <summary>
        /// Required ONLY for Adjustment transactions
        /// </summary>
        [MaxLength(500)]
        public string? Reason { get; set; }

        // =========================
        // SYSTEM
        // =========================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
