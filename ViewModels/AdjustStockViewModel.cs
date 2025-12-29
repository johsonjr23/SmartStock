namespace SmartStock.ViewModels
{
    public class AdjustStockViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        // Can be positive or negative
        public int Quantity { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
