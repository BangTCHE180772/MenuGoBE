using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class LinkCustomerToPartner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CustomerId",
                table: "Partners",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Partners_CustomerId",
                table: "Partners",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Partners_Customer_CustomerId",
                table: "Partners",
                column: "CustomerId",
                principalTable: "Customer",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Partners_Customer_CustomerId",
                table: "Partners");

            migrationBuilder.DropIndex(
                name: "IX_Partners_CustomerId",
                table: "Partners");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Partners");
        }
    }
}
