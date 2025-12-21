using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class StockHistory
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int ProductId { get; set; }

    public string ChangeType { get; set; } = null!;

    public int QuantityChange { get; set; }

    public int OldQuantity { get; set; }

    public int NewQuantity { get; set; }

    public int? ReferenceId { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Tenant Tenant { get; set; } = null!;
}
