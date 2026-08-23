using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftNightHours : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NightHours was added to the Shift model but the column is missing in the DB.
            migrationBuilder.Sql(@"
                ALTER TABLE ""Shifts""
                ADD COLUMN IF NOT EXISTS ""NightHours"" numeric(18,4) NOT NULL DEFAULT 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NightHours",
                table: "Shifts");
        }
    }
}
