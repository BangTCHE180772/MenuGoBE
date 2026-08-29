using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class Add_Batch_Management_And_Fefo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShelfLifeDays",
                table: "Products",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BatchCodeSnapshot",
                table: "DocumentDetails",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiryDateSnapshot",
                table: "DocumentDetails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ManufactureDateSnapshot",
                table: "DocumentDetails",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BInventoryBatches",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BInventoryId = table.Column<long>(type: "bigint", nullable: false),
                    BatchCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    QuantityOriginal = table.Column<decimal>(type: "numeric(51,3)", nullable: false),
                    QuantityRemaining = table.Column<decimal>(type: "numeric(51,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(108,12)", nullable: false),
                    ManufactureDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReceivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceBatchId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BInventoryBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BInventoryBatches_BInventories_BInventoryId",
                        column: x => x.BInventoryId,
                        principalTable: "BInventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BInventoryBatches_BInventoryBatches_SourceBatchId",
                        column: x => x.SourceBatchId,
                        principalTable: "BInventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BatchAllocations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocumentDetailId = table.Column<long>(type: "bigint", nullable: false),
                    BatchId = table.Column<long>(type: "bigint", nullable: false),
                    AllocationType = table.Column<string>(type: "text", nullable: false),
                    QuantityAllocated = table.Column<decimal>(type: "numeric(51,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(108,12)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchAllocations_BInventoryBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "BInventoryBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BatchAllocations_DocumentDetails_DocumentDetailId",
                        column: x => x.DocumentDetailId,
                        principalTable: "DocumentDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BatchAllocations_BatchId",
                table: "BatchAllocations",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchAllocations_DocumentDetailId",
                table: "BatchAllocations",
                column: "DocumentDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_BInventoryBatches_BInventoryId_BatchCode",
                table: "BInventoryBatches",
                columns: new[] { "BInventoryId", "BatchCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BInventoryBatches_BInventoryId_Status_ExpiryDate",
                table: "BInventoryBatches",
                columns: new[] { "BInventoryId", "Status", "ExpiryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BInventoryBatches_SourceBatchId",
                table: "BInventoryBatches",
                column: "SourceBatchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BatchAllocations");

            migrationBuilder.DropTable(
                name: "BInventoryBatches");

            migrationBuilder.DropColumn(
                name: "ShelfLifeDays",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BatchCodeSnapshot",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "ExpiryDateSnapshot",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "ManufactureDateSnapshot",
                table: "DocumentDetails");
        }
    }
}
