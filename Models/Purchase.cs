using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class Purchase
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int? SupplierId { get; set; }

    public string? InvoiceNumber { get; set; }

    public decimal? TotalAmount { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();

    public virtual Supplier? Supplier { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}
