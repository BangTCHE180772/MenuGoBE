using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddLeftoverRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LeftoverRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    OrderDetailId = table.Column<long>(type: "bigint", nullable: true),
                    HandlingAction = table.Column<int>(type: "integer", nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ShiftId = table.Column<long>(type: "bigint", nullable: true),
                    RecordDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeftoverRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeftoverRecords_Accounts_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeftoverRecords_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeftoverRecords_OrderDetails_OrderDetailId",
                        column: x => x.OrderDetailId,
                        principalTable: "OrderDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeftoverRecords_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeftoverRecords_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeftoverRecords_BranchId",
                table: "LeftoverRecords",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_LeftoverRecords_CreatedBy",
                table: "LeftoverRecords",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LeftoverRecords_OrderDetailId",
                table: "LeftoverRecords",
                column: "OrderDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_LeftoverRecords_ProductId",
                table: "LeftoverRecords",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_LeftoverRecords_ShiftId",
                table: "LeftoverRecords",
                column: "ShiftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeftoverRecords");
        }
    }
}
