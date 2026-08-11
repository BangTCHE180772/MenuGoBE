using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RefactorInventoryAndReturnItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReservedQuantity",
                table: "BInventories");

            migrationBuilder.AddColumn<long>(
                name: "ReturnConfirmedBy",
                table: "OrderDetails",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ReturnIsIntact",
                table: "OrderDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReturnReason",
                table: "OrderDetails",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturnedAt",
                table: "OrderDetails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReturnedQuantity",
                table: "OrderDetails",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "OrderId",
                table: "Documents",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReturnConfirmedBy",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReturnIsIntact",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReturnReason",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReturnedAt",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "Documents");

            migrationBuilder.AddColumn<decimal>(
                name: "ReservedQuantity",
                table: "BInventories",
                type: "numeric(51,3)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
