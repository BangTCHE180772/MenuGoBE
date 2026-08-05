using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class SyncDocumentModelsRefined : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Documents\" DROP COLUMN IF EXISTS \"Discount\";");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" DROP COLUMN IF EXISTS \"OtherCost\";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Documents",
                type: "numeric(108,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCost",
                table: "Documents",
                type: "numeric(108,12)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
