namespace SmartStock.ViewModels
{
    public class ProductMarginRowViewModel
    {
        public string ProductName { get; set; } = string.Empty;

        public int TotalQuantity { get; set; }

        public decimal Revenue { get; set; }

        public decimal Cost { get; set; }

        public decimal Profit { get; set; }

        public decimal MarginPercentage { get; set; }

    }
}
