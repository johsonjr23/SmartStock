using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class PurchaseItem
{
    public int Id { get; set; }

    public int PurchaseId { get; set; }

    public int TenantId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal BuyingPrice { get; set; }

    public decimal? SubTotal { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Purchase Purchase { get; set; } = null!;
}
