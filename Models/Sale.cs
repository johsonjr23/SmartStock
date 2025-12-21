using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class Sale
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string? InvoiceNumber { get; set; }

    public decimal TotalAmount { get; set; }

    public string PaymentType { get; set; } = "Cash";

    public DateTime SaleDate { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public virtual Tenant Tenant { get; set; } = null!;
}
