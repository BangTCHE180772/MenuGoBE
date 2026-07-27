using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCashFlowAndDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"CashFlowDetails\" DROP CONSTRAINT IF EXISTS \"FK_CashFlowDetails_Accounts_DeletedBy\";");
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_CashFlowDetails_DeletedBy\";");
            migrationBuilder.Sql("ALTER TABLE \"CashFlowDetails\" DROP COLUMN IF EXISTS \"CreatedAt\";");
            migrationBuilder.Sql("ALTER TABLE \"CashFlowDetails\" DROP COLUMN IF EXISTS \"DeletedBy\";");

            migrationBuilder.AddColumn<DateTime>(
                name: "BusinessDate",
                table: "CashFlows",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "CashFlows",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DocumentId",
                table: "CashFlows",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "CashFlows",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CashFlows",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlows_DocumentId",
                table: "CashFlows",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashFlows_Documents_DocumentId",
                table: "CashFlows",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashFlows_Documents_DocumentId",
                table: "CashFlows");

            migrationBuilder.DropIndex(
                name: "IX_CashFlows_DocumentId",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "BusinessDate",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CashFlows");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "CashFlowDetails",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<long>(
                name: "DeletedBy",
                table: "CashFlowDetails",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowDetails_DeletedBy",
                table: "CashFlowDetails",
                column: "DeletedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_CashFlowDetails_Accounts_DeletedBy",
                table: "CashFlowDetails",
                column: "DeletedBy",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
