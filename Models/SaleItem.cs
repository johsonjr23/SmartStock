using System;

namespace SmartStock.Models
{
    public partial class SaleItem
    {
        public int Id { get; set; }

        // Parent sale
        public int SaleId { get; set; }

        // Multi-tenant safety (redundant but intentional)
        public int TenantId { get; set; }

        // Product sold
        public int ProductId { get; set; }

        // Quantity sold
        public int Quantity { get; set; }

        // Price per unit at time of sale
        public decimal SellingPrice { get; set; }

        // ✅ Phase 2: freeze cost at the time of sale
        // Nullable to avoid breaking existing historical rows after migration.
        // New sales MUST populate this.
        public decimal? BuyingPriceAtSale { get; set; }

        // Quantity * UnitPrice
        public decimal SubTotal { get; set; }

        // System timestamp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Product Product { get; set; } = null!;
        public virtual Sale Sale { get; set; } = null!;
    }
}
