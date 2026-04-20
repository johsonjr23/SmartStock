using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStock.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShopName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OwnerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    BusinessType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(sysdatetime())"),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdminNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantRequests_Status",
                table: "TenantRequests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantRequests");
        }
    }
}
