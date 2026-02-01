using System;
using System.Collections.Generic;

namespace SmartStock.ViewModels.Reports
{
    public sealed class InventoryValuationRowVm
    {
        public int ProductId { get; set; }
        public string? SKU { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";

        public decimal StockQty { get; set; }
        public decimal AvgCost { get; set; }
        public decimal InventoryValue { get; set; }
    }

    public sealed class InventoryValuationLabelsVm
    {
        public string Title { get; set; } = "Inventory Valuation";
        public string Subtitle { get; set; } = "Current stock value based on average cost";

        public string Category { get; set; } = "Category";
        public string Search { get; set; } = "Search";
        public string IncludeZero { get; set; } = "Include zero stock";
        public string Apply { get; set; } = "Apply";
        public string Reset { get; set; } = "Reset";

        public string ColSku { get; set; } = "SKU";
        public string ColProduct { get; set; } = "Product";
        public string ColCategory { get; set; } = "Category";
        public string ColStockQty { get; set; } = "Stock Qty";
        public string ColAvgCost { get; set; } = "Average Cost";
        public string ColStockValue { get; set; } = "Stock Value";

        public string TotalUnits { get; set; } = "Total Units";
        public string TotalValue { get; set; } = "Total Stock Value";
        public string NoItems { get; set; } = "No items found.";
        public string AllCategories { get; set; } = "-- All --";
        public string GeneratedAt { get; set; } = "Generated";
    }

    public sealed class InventoryValuationReportVm
    {
        public int? CategoryId { get; set; }
        public bool IncludeZero { get; set; }
        public string? Query { get; set; }
        public string? Lang { get; set; }

        public DateTimeOffset GeneratedAtUtc { get; set; }

        public List<InventoryValuationRowVm> Items { get; set; } = new();

        public decimal TotalStockUnits { get; set; }
        public decimal TotalInventoryValue { get; set; }

        public InventoryValuationLabelsVm Labels { get; set; } = new();
    }
}
