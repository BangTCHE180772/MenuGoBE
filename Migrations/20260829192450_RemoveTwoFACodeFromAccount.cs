using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTwoFACodeFromAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF EXISTS: cột này đã được xóa thủ công trên DB dùng chung trước khi
            // migration này được tạo, nên phải idempotent cho các máy khác chưa xóa.
            migrationBuilder.Sql(@"ALTER TABLE ""Accounts"" DROP COLUMN IF EXISTS ""TwoFACode"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TwoFACode",
                table: "Accounts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
