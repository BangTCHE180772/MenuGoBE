using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentDetailStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DocumentDetails",
                type: "text",
                nullable: false,
                defaultValue: "Completed");

            migrationBuilder.Sql(@"
                UPDATE ""DocumentDetails""
                SET ""Status"" = CASE WHEN ""IsDeleted"" = true THEN 'Cancelled' ELSE 'Completed' END;
            ");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DocumentDetails");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DocumentDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(@"
                UPDATE ""DocumentDetails""
                SET ""IsDeleted"" = CASE WHEN ""Status"" = 'Cancelled' THEN true ELSE false END;
            ");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DocumentDetails");
        }
    }
}
