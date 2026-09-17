using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddAtFaultAccountToLeftoverRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AtFaultAccountId",
                table: "LeftoverRecords",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeftoverRecords_AtFaultAccountId",
                table: "LeftoverRecords",
                column: "AtFaultAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_LeftoverRecords_Accounts_AtFaultAccountId",
                table: "LeftoverRecords",
                column: "AtFaultAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LeftoverRecords_Accounts_AtFaultAccountId",
                table: "LeftoverRecords");

            migrationBuilder.DropIndex(
                name: "IX_LeftoverRecords_AtFaultAccountId",
                table: "LeftoverRecords");

            migrationBuilder.DropColumn(
                name: "AtFaultAccountId",
                table: "LeftoverRecords");
        }
    }
}
