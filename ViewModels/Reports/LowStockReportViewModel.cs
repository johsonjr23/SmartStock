using System.Collections.Generic;

namespace SmartStock.ViewModels.Reports
{
    public class LowStockRowVm
    {
        public int ProductId { get; set; }
        public string? SKU { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public decimal CurrentStock { get; set; }
        public int? ReorderLevel { get; set; }
        public bool IsCritical => CurrentStock <= 0;
    }

    public class LowStockReportViewModel
    {
        public string TenantName { get; set; } = "";
        public System.DateTimeOffset GeneratedAtUtc { get; set; }
        public List<LowStockRowVm> Items { get; set; } = new();

        public int CriticalCount => Items.Count(i => i.IsCritical);
        public int LowCount => Items.Count(i => !i.IsCritical);
    }
}
