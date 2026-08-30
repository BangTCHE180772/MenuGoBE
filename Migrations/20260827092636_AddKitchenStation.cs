using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddKitchenStation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "KitchenStationId",
                table: "Products",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KitchenStations",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KitchenStations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KitchenStations_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_KitchenStationId",
                table: "Products",
                column: "KitchenStationId");

            migrationBuilder.CreateIndex(
                name: "IX_KitchenStations_BranchId_Name",
                table: "KitchenStations",
                columns: new[] { "BranchId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_KitchenStations_KitchenStationId",
                table: "Products",
                column: "KitchenStationId",
                principalTable: "KitchenStations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_KitchenStations_KitchenStationId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "KitchenStations");

            migrationBuilder.DropIndex(
                name: "IX_Products_KitchenStationId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "KitchenStationId",
                table: "Products");
        }
    }
}
