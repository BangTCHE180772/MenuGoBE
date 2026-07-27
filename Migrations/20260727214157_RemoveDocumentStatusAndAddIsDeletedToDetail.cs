using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentStatusAndAddIsDeletedToDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Drop Status column from Documents table
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Documents");

            // 2. Drop Status column from DocumentDetails table
            migrationBuilder.DropColumn(
                name: "Status",
                table: "DocumentDetails");

            // 3. Add IsDeleted column to DocumentDetails table (bool, default false)
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DocumentDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: remove IsDeleted from DocumentDetails
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DocumentDetails");

            // Reverse: re-add Status to DocumentDetails (nullable int)
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "DocumentDetails",
                type: "integer",
                nullable: true);

            // Reverse: re-add Status to Documents (required text)
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
