using System;
using System.Collections.Generic;

namespace SmartStock.ViewModels
{
    public class ProfitReportViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // Expenses
        public decimal TotalExpenses { get; set; }
        public List<ExpenseByTypeRow> ExpensesByType { get; set; } = new();

        // Sales / Refunds (Revenue)
        public decimal GrossSalesAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal NetSalesAmount { get; set; }

        // COGS & Profit
        public decimal SalesCOGS { get; set; }
        public decimal RefundCOGS { get; set; }
        public decimal NetCOGS { get; set; }

        public decimal GrossProfit { get; set; }
        public decimal NetProfit { get; set; }
    }

    public class ExpenseByTypeRow
    {
        public string Type { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
