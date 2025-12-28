using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class Product
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public int? CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? SKU { get; set; }

    public decimal BuyingPrice { get; set; }

    public decimal SellingPrice { get; set; }

    

    public string? Unit { get; set; }

    public DateTime CreatedAt { get; set; }

    public int? UnitId { get; set; }

    public int? ReorderLevel { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();

    public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

    public virtual ICollection<StockHistory> StockHistories { get; set; } = new List<StockHistory>();

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual Unit? UnitNavigation { get; set; }
}
