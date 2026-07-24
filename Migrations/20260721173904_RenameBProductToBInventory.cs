using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RenameBProductToBInventory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"BProducts\" CASCADE;");

            migrationBuilder.RenameColumn(
                name: "ProductId",
                table: "InventoryLedgers",
                newName: "BInventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryLedgers_ProductId",
                table: "InventoryLedgers",
                newName: "IX_InventoryLedgers_BInventoryId");

            migrationBuilder.RenameColumn(
                name: "BProductId",
                table: "DocumentDetails",
                newName: "BInventoryId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentDetails_BProductId",
                table: "DocumentDetails",
                newName: "IX_DocumentDetails_BInventoryId");

            migrationBuilder.CreateTable(
                name: "BInventories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Avg = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    LeftOver = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(21,3)", nullable: false),
                    ChainActive = table.Column<bool>(type: "boolean", nullable: false),
                    BranchActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BInventories_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BInventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BInventories_BranchId",
                table: "BInventories",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BInventories_ProductId",
                table: "BInventories",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentDetails_BInventories_BInventoryId",
                table: "DocumentDetails",
                column: "BInventoryId",
                principalTable: "BInventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLedgers_BInventories_BInventoryId",
                table: "InventoryLedgers",
                column: "BInventoryId",
                principalTable: "BInventories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentDetails_BInventories_BInventoryId",
                table: "DocumentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLedgers_BInventories_BInventoryId",
                table: "InventoryLedgers");

            migrationBuilder.DropTable(
                name: "BInventories");

            migrationBuilder.RenameColumn(
                name: "BInventoryId",
                table: "InventoryLedgers",
                newName: "ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_InventoryLedgers_BInventoryId",
                table: "InventoryLedgers",
                newName: "IX_InventoryLedgers_ProductId");

            migrationBuilder.RenameColumn(
                name: "BInventoryId",
                table: "DocumentDetails",
                newName: "BProductId");

            migrationBuilder.RenameIndex(
                name: "IX_DocumentDetails_BInventoryId",
                table: "DocumentDetails",
                newName: "IX_DocumentDetails_BProductId");

            migrationBuilder.CreateTable(
                name: "BProducts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Avg = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    BranchActive = table.Column<bool>(type: "boolean", nullable: false),
                    ChainActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LeftOver = table.Column<decimal>(type: "numeric(30,12)", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(21,3)", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BProducts_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BProducts_BranchId",
                table: "BProducts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_BProducts_ProductId",
                table: "BProducts",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentDetails_BProducts_BProductId",
                table: "DocumentDetails",
                column: "BProductId",
                principalTable: "BProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLedgers_BProducts_ProductId",
                table: "InventoryLedgers",
                column: "ProductId",
                principalTable: "BProducts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
