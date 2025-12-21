using System;
using System.Collections.Generic;

namespace SmartStock.Models;

public partial class AppUser
{
    public int Id { get; set; }

    public int TenantId { get; set; }

    public string Username { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string Role { get; set; } = null!;

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}
