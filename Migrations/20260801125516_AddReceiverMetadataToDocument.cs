using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiverMetadataToDocument : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ReceiverAccountId",
                table: "Documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotReceiverName",
                table: "Documents",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnapshotReceiverUsername",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_ReceiverAccountId",
                table: "Documents",
                column: "ReceiverAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_Accounts_ReceiverAccountId",
                table: "Documents",
                column: "ReceiverAccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Accounts_ReceiverAccountId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ReceiverAccountId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ReceiverAccountId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotReceiverName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotReceiverUsername",
                table: "Documents");
        }
    }
}
