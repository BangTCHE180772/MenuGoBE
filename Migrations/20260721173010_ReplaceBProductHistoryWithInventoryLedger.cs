using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceBProductHistoryWithInventoryLedger : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"BProductHistories\" CASCADE;");

            migrationBuilder.CreateTable(
                name: "InventoryLedgers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentId = table.Column<long>(type: "bigint", nullable: true),
                    DocumentDetailId = table.Column<long>(type: "bigint", nullable: true),
                    PostingSequence = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BusinessDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DocumentType = table.Column<string>(type: "text", nullable: false),
                    QuantityDelta = table.Column<decimal>(type: "numeric(21,3)", nullable: false),
                    InventoryValueDelta = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    RunningQuantity = table.Column<decimal>(type: "numeric(21,3)", nullable: false),
                    RunningInventoryValue = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    RunningAverageCost = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    RunningLeftOver = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    ActualQuantity = table.Column<decimal>(type: "numeric(21,3)", nullable: true),
                    VarianceQuantity = table.Column<decimal>(type: "numeric(21,3)", nullable: true),
                    UnitCost = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryLedgers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_Accounts_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_BProducts_ProductId",
                        column: x => x.ProductId,
                        principalTable: "BProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_DocumentDetails_DocumentDetailId",
                        column: x => x.DocumentDetailId,
                        principalTable: "DocumentDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InventoryLedgers_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_CreatedBy",
                table: "InventoryLedgers",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_DocumentDetailId",
                table: "InventoryLedgers",
                column: "DocumentDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_DocumentId",
                table: "InventoryLedgers",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_ProductId",
                table: "InventoryLedgers",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryLedgers");

            migrationBuilder.CreateTable(
                name: "BProductHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BProductId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentDetailId = table.Column<long>(type: "bigint", nullable: true),
                    AvgAfter = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    AvgBefore = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    ChangeType = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(21,3)", nullable: false),
                    QuantityAfter = table.Column<decimal>(type: "numeric(21,3)", nullable: false),
                    QuantityBefore = table.Column<decimal>(type: "numeric(21,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BProductHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BProductHistories_BProducts_BProductId",
                        column: x => x.BProductId,
                        principalTable: "BProducts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BProductHistories_DocumentDetails_DocumentDetailId",
                        column: x => x.DocumentDetailId,
                        principalTable: "DocumentDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BProductHistories_BProductId",
                table: "BProductHistories",
                column: "BProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BProductHistories_DocumentDetailId",
                table: "BProductHistories",
                column: "DocumentDetailId");
        }
    }
}
