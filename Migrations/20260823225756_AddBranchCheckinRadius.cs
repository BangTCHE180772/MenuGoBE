using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchCheckinRadius : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NightHours (Shifts) and Latitude/Longitude (Branches) already exist in the DB.
            // Only add CheckinRadiusMeters which is the newly added column.
            migrationBuilder.AddColumn<int>(
                name: "CheckinRadiusMeters",
                table: "Branches",
                type: "integer",
                nullable: false,
                defaultValue: 100);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckinRadiusMeters",
                table: "Branches");
        }
    }
}
