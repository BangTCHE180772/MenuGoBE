using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBranchAndChainCoordinatesAndRemoveAddressText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressText",
                table: "Chains");

            migrationBuilder.DropColumn(
                name: "AddressText",
                table: "Branches");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Chains",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Chains",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Chains");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Chains");

            migrationBuilder.AddColumn<string>(
                name: "AddressText",
                table: "Chains",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AddressText",
                table: "Branches",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
