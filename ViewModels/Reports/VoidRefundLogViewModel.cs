using System;
using System.Collections.Generic;

namespace SmartStock.ViewModels.Reports
{
    public class VoidRefundRowVm
    {
        public int SaleId { get; set; }
        public string? InvoiceNumber { get; set; }
        public string Type { get; set; } = "";   // "Voided" | "Refunded"
        public DateTime EventDate { get; set; }
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
    }

    public class VoidRefundLogViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public string TenantName { get; set; } = "";
        public DateTimeOffset GeneratedAtUtc { get; set; }

        public List<VoidRefundRowVm> Rows { get; set; } = new();

        public int VoidCount => Rows.Count(r => r.Type == "Voided");
        public int RefundCount => Rows.Count(r => r.Type == "Refunded");
        public decimal TotalVoidedAmount => Rows.Where(r => r.Type == "Voided").Sum(r => r.Amount);
        public decimal TotalRefundedAmount => Rows.Where(r => r.Type == "Refunded").Sum(r => r.Amount);
    }
}
