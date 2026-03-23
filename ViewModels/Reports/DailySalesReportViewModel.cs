using System;
using System.Collections.Generic;

namespace SmartStock.ViewModels.Reports
{
    public class DailySalesRowVm
    {
        public DateTime Date { get; set; }
        public int Transactions { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AveragePerSale => Transactions == 0 ? 0 : TotalRevenue / Transactions;
    }

    public class DailySalesReportViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string TenantName { get; set; } = "";
        public DateTimeOffset GeneratedAtUtc { get; set; }

        public List<DailySalesRowVm> Rows { get; set; } = new();

        public int TotalTransactions => Rows.Sum(r => r.Transactions);
        public decimal TotalRevenue => Rows.Sum(r => r.TotalRevenue);
        public int ActiveDays => Rows.Count(r => r.Transactions > 0);
        public decimal AverageDailyRevenue => ActiveDays == 0 ? 0 : TotalRevenue / ActiveDays;
    }
}
