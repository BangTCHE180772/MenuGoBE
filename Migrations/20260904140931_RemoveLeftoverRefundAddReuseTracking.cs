using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLeftoverRefundAddReuseTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundAmount",
                table: "LeftoverRecords");

            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAt",
                table: "LeftoverRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UsedByOrderDetailId",
                table: "LeftoverRecords",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeftoverRecords_UsedByOrderDetailId",
                table: "LeftoverRecords",
                column: "UsedByOrderDetailId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeftoverRecords_OrderDetails_UsedByOrderDetailId",
                table: "LeftoverRecords",
                column: "UsedByOrderDetailId",
                principalTable: "OrderDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeftoverRecords_OrderDetails_UsedByOrderDetailId",
                table: "LeftoverRecords");

            migrationBuilder.DropIndex(
                name: "IX_LeftoverRecords_UsedByOrderDetailId",
                table: "LeftoverRecords");

            migrationBuilder.DropColumn(
                name: "UsedAt",
                table: "LeftoverRecords");

            migrationBuilder.DropColumn(
                name: "UsedByOrderDetailId",
                table: "LeftoverRecords");

            migrationBuilder.AddColumn<decimal>(
                name: "RefundAmount",
                table: "LeftoverRecords",
                type: "numeric(18,4)",
                nullable: true);
        }
    }
}
