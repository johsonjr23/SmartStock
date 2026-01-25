using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartStock.Migrations
{
    /// <inheritdoc />
    public partial class Step5_AddRefundFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalSaleId",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefundReason",
                table: "Sales",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RefundedAt",
                table: "Sales",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefundedBy",
                table: "Sales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_OriginalSaleId",
                table: "Sales",
                column: "OriginalSaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Sales_OriginalSaleId",
                table: "Sales",
                column: "OriginalSaleId",
                principalTable: "Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Sales_OriginalSaleId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_OriginalSaleId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "OriginalSaleId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RefundReason",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RefundedAt",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "RefundedBy",
                table: "Sales");
        }
    }
}
