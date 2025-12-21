using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class SaleItem
{
    public int Id { get; set; }

    public int SaleId { get; set; }

    public int TenantId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal SellingPrice { get; set; }

    public decimal SubTotal { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual Sale Sale { get; set; } = null!;
}
