using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIsLeftOverFromDocumentDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" DROP COLUMN IF EXISTS \"IsLeftOver\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLeftOver",
                table: "DocumentDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
