using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyDocumentCashFlowArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLedgers_DocumentDetails_DocumentDetailId",
                table: "InventoryLedgers");

            migrationBuilder.DropTable(
                name: "CashFlowDetails");

            migrationBuilder.DropTable(
                name: "DocumentPartners");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLedgers_DocumentDetailId",
                table: "InventoryLedgers");

            migrationBuilder.DropColumn(
                name: "DocumentDetailId",
                table: "InventoryLedgers");

            migrationBuilder.AddColumn<decimal>(
                name: "AmountDue",
                table: "Documents",
                type: "numeric(108,12)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Documents",
                type: "numeric(108,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "PartnerId",
                table: "Documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "CashFlows",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PartnerId",
                table: "CashFlows",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "CashFlows",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "CashFlows",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Documents_PartnerId",
                table: "Documents",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlows_PartnerId",
                table: "CashFlows",
                column: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashFlows_Partners_PartnerId",
                table: "CashFlows",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Partners_PartnerId",
                table: "Documents",
                column: "PartnerId",
                principalTable: "Partners",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashFlows_Partners_PartnerId",
                table: "CashFlows");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Partners_PartnerId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_PartnerId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_CashFlows_PartnerId",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "AmountDue",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "PartnerId",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "CashFlows");

            migrationBuilder.AddColumn<long>(
                name: "DocumentDetailId",
                table: "InventoryLedgers",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocumentPartners",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentId = table.Column<long>(type: "bigint", nullable: false),
                    PartnerId = table.Column<long>(type: "bigint", nullable: false),
                    Cost = table.Column<decimal>(type: "numeric(108,12)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentPartners", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentPartners_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DocumentPartners_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CashFlowDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CashFlowId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentPartnerId = table.Column<long>(type: "bigint", nullable: true),
                    PartnerId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(108,12)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    PaymentMethod = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashFlowDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashFlowDetails_CashFlows_CashFlowId",
                        column: x => x.CashFlowId,
                        principalTable: "CashFlows",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashFlowDetails_DocumentPartners_DocumentPartnerId",
                        column: x => x.DocumentPartnerId,
                        principalTable: "DocumentPartners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CashFlowDetails_Partners_PartnerId",
                        column: x => x.PartnerId,
                        principalTable: "Partners",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_DocumentDetailId",
                table: "InventoryLedgers",
                column: "DocumentDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowDetails_CashFlowId",
                table: "CashFlowDetails",
                column: "CashFlowId");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowDetails_DocumentPartnerId",
                table: "CashFlowDetails",
                column: "DocumentPartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_CashFlowDetails_PartnerId",
                table: "CashFlowDetails",
                column: "PartnerId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPartners_DocumentId",
                table: "DocumentPartners",
                column: "DocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPartners_PartnerId",
                table: "DocumentPartners",
                column: "PartnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLedgers_DocumentDetails_DocumentDetailId",
                table: "InventoryLedgers",
                column: "DocumentDetailId",
                principalTable: "DocumentDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
