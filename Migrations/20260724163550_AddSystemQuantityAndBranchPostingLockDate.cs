using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemQuantityAndBranchPostingLockDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SystemQuantity",
                table: "DocumentDetails",
                type: "numeric(21,3)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PostingLockDate",
                table: "Branches",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SystemQuantity",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "PostingLockDate",
                table: "Branches");
        }
    }
}
