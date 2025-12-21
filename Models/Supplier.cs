using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class Supplier
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string Name { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

    public virtual Tenant Tenant { get; set; } = null!;
}
