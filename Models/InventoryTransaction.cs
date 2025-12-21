using System;
using System.ComponentModel.DataAnnotations;

namespace SmartStock.Models
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public int QuantityChange { get; set; }

        [Required]
        public InventoryTransactionType TransactionType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Required]
        public int TenantId { get; set; }

        public Product Product { get; set; }


        public int? SaleId { get; set; }

    }
}
