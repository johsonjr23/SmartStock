using System;

namespace SmartStock.ReportsLocalization
{
    public static class ReportLabelProvider
    {
        public static ReportLabels Get(string? lang)
        {
            lang = (lang ?? "en").ToLower();

            return lang == "sw"
                ? Swahili()
                : English();
        }

        private static ReportLabels English() => new()
        {
            ReportTitle = "Profit Report",
            FromDate = "From Date",
            ToDate = "To Date",
            Generate = "Generate",

            GrossSales = "Gross Sales",
            Refunds = "Refunds",
            NetSales = "Net Sales",

            TotalExpenses = "Total Expenses",
            GrossProfit = "Gross Profit",
            NetProfit = "Net Profit",

            ExpenseType = "Expense Type",
            Amount = "Amount",
            NoData = "No data for selected period"
        };

        private static ReportLabels Swahili() => new()
        {
            ReportTitle = "Ripoti ya Faida",
            FromDate = "Kuanzia Tarehe",
            ToDate = "Hadi Tarehe",
            Generate = "Tengeneza",

            GrossSales = "Mauzo Jumla",
            Refunds = "Marejesho",
            NetSales = "Mauzo Halisi",

            TotalExpenses = "Jumla ya Gharama",
            GrossProfit = "Faida Kabla ya Gharama",
            NetProfit = "Faida Halisi",

            ExpenseType = "Aina ya Gharama",
            Amount = "Kiasi",
            NoData = "Hakuna data kwa kipindi kilichochaguliwa"
        };
    }
}
