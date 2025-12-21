using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class Tenant
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? OwnerName { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? SubscriptionPlan { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AppUser> AppUsers { get; set; } = new List<AppUser>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    public virtual ICollection<StockHistory> StockHistories { get; set; } = new List<StockHistory>();

    public virtual ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
}
