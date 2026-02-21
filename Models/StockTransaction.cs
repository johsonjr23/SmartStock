namespace SmartStock.Models
{
    public class StockTransaction
    {
        public int Id { get; set; }

        public int TenantId { get; set; }
        public int ProductId { get; set; }

        public int Quantity { get; set; } // positive or negative

        public string Type { get; set; } // OPENING, SALE, REFUND, VOID, ADJUSTMENT

        public string Reference { get; set; } // e.g., SaleId, RefundId

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Product Product { get; set; }
    }
}
