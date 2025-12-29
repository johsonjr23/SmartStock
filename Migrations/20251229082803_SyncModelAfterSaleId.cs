using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStock.Migrations
{
    public partial class SyncModelAfterSaleId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline sync migration.
            // All schema changes already exist in the database.
            // This migration exists only to align EF Core model snapshot.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally left empty.
            // We do not roll back baseline schema.
        }
    }
}
