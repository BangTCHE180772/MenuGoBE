using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RenameLeftoverDiscountToCompensationPoints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "LeftoverRecords");

            migrationBuilder.AddColumn<int>(
                name: "CompensationPoints",
                table: "LeftoverRecords",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompensationPoints",
                table: "LeftoverRecords");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "LeftoverRecords",
                type: "numeric(18,4)",
                nullable: true);
        }
    }
}
