using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddPostingSequenceToInventoryAndCashFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashFlows_Documents_DocumentId",
                table: "CashFlows");

            migrationBuilder.DropIndex(
                name: "IX_CashFlows_DocumentId",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "CashFlows");

            migrationBuilder.AlterColumn<long>(
                name: "PostingSequence",
                table: "InventoryLedgers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<long>(
                name: "PostingSequence",
                table: "CashFlows",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostingSequence",
                table: "CashFlows");

            migrationBuilder.AlterColumn<long>(
                name: "PostingSequence",
                table: "InventoryLedgers",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<long>(
                name: "DocumentId",
                table: "CashFlows",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CashFlows_DocumentId",
                table: "CashFlows",
                column: "DocumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_CashFlows_Documents_DocumentId",
                table: "CashFlows",
                column: "DocumentId",
                principalTable: "Documents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
