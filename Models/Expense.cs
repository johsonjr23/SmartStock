using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartStock.Models
{
    public class Expense
    {
        public int Id { get; set; }

        // Multi-tenant ownership
        public int TenantId { get; set; }

        [Required, StringLength(100)]
        public string Type { get; set; } = ""; // e.g. Rent, Transport, Internet, Salaries

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, 999999999)]
        public decimal Amount { get; set; }

        // Optional "to whom went"
        [StringLength(200)]
        public string? Payee { get; set; }

        // Optional extra details
        [StringLength(500)]
        public string? Notes { get; set; }

        // Business date for the expense (important for reports)
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        // Optional: who created it (your system already has CreatedBy pattern)
        public int? CreatedBy { get; set; }

        // System timestamp
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
