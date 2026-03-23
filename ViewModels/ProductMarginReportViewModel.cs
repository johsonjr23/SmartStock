namespace SmartStock.ViewModels
{
    public class ProductMarginReportViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string TenantName { get; set; } = "";
        public List<ProductMarginRowViewModel> Items { get; set; } = new();
    }
}
