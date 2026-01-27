using System;
using System.ComponentModel.DataAnnotations;

namespace SmartStock.ViewModels
{
    public class CreateExpenseViewModel
    {
        [Required, StringLength(100)]
        public string Type { get; set; } = "";

        [Range(0.01, 999999999)]
        public decimal Amount { get; set; }

        [StringLength(200)]
        public string? Payee { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [DataType(DataType.Date)]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;
    }
}
