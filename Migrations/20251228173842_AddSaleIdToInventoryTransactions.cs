using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStock.Migrations
{
    public partial class AddSaleIdToInventoryTransactions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // BASELINE MIGRATION
            // InventoryTransactions, SaleId, SellingPrice already exist in DB.
            // Identity tables are managed by SmartStockIdentityDbContext.
            // No schema changes should occur here.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty.
            // We do not rollback baseline schema.
        }
    }
}
