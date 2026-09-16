using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class MultiOrderAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderAssignments_OrderId",
                table: "OrderAssignments");

            migrationBuilder.AddColumn<string>(
                name: "AssignmentType",
                table: "OrderAssignments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_IsActive",
                table: "OrderAssignments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_OrderId_AccountId_AssignmentType",
                table: "OrderAssignments",
                columns: new[] { "OrderId", "AccountId", "AssignmentType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_OrderAssignments_IsActive",
                table: "OrderAssignments");

            migrationBuilder.DropIndex(
                name: "IX_OrderAssignments_OrderId_AccountId_AssignmentType",
                table: "OrderAssignments");

            migrationBuilder.DropColumn(
                name: "AssignmentType",
                table: "OrderAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAssignments_OrderId",
                table: "OrderAssignments",
                column: "OrderId",
                unique: true);
        }
    }
}
