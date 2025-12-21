using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStock.Migrations.SmartStockIdentityDb
{
    /// <inheritdoc />
    public partial class ForceAddTenantIdToAspNetUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
       name: "TenantId",
       table: "AspNetUsers",
       type: "int",
       nullable: false,
       defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
       name: "TenantId",
       table: "AspNetUsers");
        }
    }
}
