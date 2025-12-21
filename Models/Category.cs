using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class Category
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();

    public virtual Tenant Tenant { get; set; } = null!;
}
