using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCompensationPointsFromLeftoverRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompensationPoints",
                table: "LeftoverRecords");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompensationPoints",
                table: "LeftoverRecords",
                type: "integer",
                nullable: true);
        }
    }
}
