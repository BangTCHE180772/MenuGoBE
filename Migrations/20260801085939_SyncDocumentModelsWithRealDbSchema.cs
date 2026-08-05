using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class SyncDocumentModelsWithRealDbSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "DocumentId",
                table: "InventoryLedgers",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "DocumentDetailId",
                table: "InventoryLedgers",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"IsDeleted\" boolean NOT NULL DEFAULT false;");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"IsDeleted\" boolean NOT NULL DEFAULT false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DocumentDetails");

            migrationBuilder.AlterColumn<long>(
                name: "DocumentId",
                table: "InventoryLedgers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "DocumentDetailId",
                table: "InventoryLedgers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
