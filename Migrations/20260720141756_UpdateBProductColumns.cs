using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBProductColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "LeftOver",
                table: "BProducts",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(21,3)");

            migrationBuilder.AddColumn<decimal>(
                name: "Quantity",
                table: "BProducts",
                type: "numeric(21,3)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "BProducts");

            migrationBuilder.AlterColumn<decimal>(
                name: "LeftOver",
                table: "BProducts",
                type: "numeric(21,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");
        }
    }
}
