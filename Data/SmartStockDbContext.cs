using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SmartStock.Models;

namespace SmartStock.Data;

// DOMAIN DbContext ONLY (NO IDENTITY HERE)
public partial class SmartStockDbContext : DbContext
{
    public SmartStockDbContext()
    {
    }

    public SmartStockDbContext(DbContextOptions<SmartStockDbContext> options)
        : base(options)
    {
    }

    // ===== DOMAIN TABLES =====

    public virtual DbSet<Category> Categories { get; set; }
    public virtual DbSet<Product> Products { get; set; }
    public virtual DbSet<Purchase> Purchases { get; set; }
    public virtual DbSet<PurchaseItem> PurchaseItems { get; set; }
    public virtual DbSet<Sale> Sales { get; set; }
    public virtual DbSet<SaleItem> SaleItems { get; set; }
    public virtual DbSet<StockHistory> StockHistories { get; set; }
    public virtual DbSet<Supplier> Suppliers { get; set; }
    public virtual DbSet<Tenant> Tenants { get; set; }
    public virtual DbSet<Unit> Units { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<Expense> Expenses { get; set; }

    public DbSet<StockTransaction> StockTransactions { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code.
        => optionsBuilder.UseSqlServer("Server=localhost;Database=SmartStockDb;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Name).HasMaxLength(200);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Categories)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.TenantId, e.Name });

            entity.Property(e => e.BuyingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.ReorderLevel).HasDefaultValue(0);
            entity.Property(e => e.SKU).HasMaxLength(100);
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Unit).HasMaxLength(50);

            entity.HasOne(d => d.Category).WithMany(p => p.Products)
                .HasForeignKey(d => d.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Products)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.UnitNavigation).WithMany(p => p.Products)
                .HasForeignKey(d => d.UnitId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Purchase>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Supplier).WithMany(p => p.Purchases)
                .HasForeignKey(d => d.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Purchases)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PurchaseItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BuyingPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.SubTotal)
                .HasComputedColumnSql("([Quantity]*[BuyingPrice])", false)
                .HasColumnType("decimal(29, 2)");

            // Keep: Purchase -> PurchaseItems cascade (owned children)
            entity.HasOne(d => d.Purchase).WithMany(p => p.PurchaseItems)
                .HasForeignKey(d => d.PurchaseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Block: Product delete if it has purchase history
            entity.HasOne(d => d.Product).WithMany(p => p.PurchaseItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.InvoiceNumber).HasMaxLength(100);
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.RefundReason).HasMaxLength(500);

            entity.HasOne(e => e.OriginalSale)
                .WithMany()
                .HasForeignKey(e => e.OriginalSaleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Step 5 fields
            entity.Property(e => e.Status)
                .HasConversion<int>()
                .HasDefaultValue(SaleStatus.Completed);

            entity.Property(e => e.VoidReason).HasMaxLength(500);

            // NOTE: Tenant navigation intentionally removed
        });


        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.SellingPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.BuyingPriceAtSale).HasColumnType("decimal(18,2)");


            entity.Property(e => e.SubTotal)
                .HasComputedColumnSql("([Quantity]*[SellingPrice])", false)
                .HasColumnType("decimal(29, 2)");

            // ...
        });




        modelBuilder.Entity<StockHistory>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ChangeType).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.Remarks).HasMaxLength(255);

            entity.HasOne(d => d.Product).WithMany(p => p.StockHistories)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Tenant).WithMany(p => p.StockHistories)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Address).HasMaxLength(400);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);

            entity.HasOne(d => d.Tenant).WithMany(p => p.Suppliers)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.OwnerName).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.SubscriptionPlan).HasMaxLength(100);
        });

        modelBuilder.Entity<Unit>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.Name).HasMaxLength(100);
        });

        modelBuilder.Entity<InventoryTransaction>(entity =>
        {
            entity.HasIndex(e => new { e.TenantId, e.ProductId });

            // Explicit relationships (safer for Step 5 reversals and prevents accidental deletes)
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Sale)
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())");
            entity.HasIndex(e => new { e.TenantId, e.ExpenseDate });
        });


    }

}
