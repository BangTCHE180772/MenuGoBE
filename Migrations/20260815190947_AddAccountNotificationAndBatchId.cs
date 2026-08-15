using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountNotificationAndBatchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryDetails_Payrolls_PayrollId",
                table: "SalaryDetails");

            migrationBuilder.AddColumn<string>(
                name: "BatchId",
                table: "OrderDetails",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AccountId",
                table: "Notifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BaseWorkDay",
                table: "Contracts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryDetails_CreatedBy",
                table: "SalaryDetails",
                column: "CreatedBy");

            migrationBuilder.Sql("DELETE FROM \"SalaryDetails\" WHERE \"CreatedBy\" NOT IN (SELECT \"Id\" FROM \"Accounts\")");

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryDetails_Accounts_CreatedBy",
                table: "SalaryDetails",
                column: "CreatedBy",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryDetails_Payrolls_PayrollId",
                table: "SalaryDetails",
                column: "PayrollId",
                principalTable: "Payrolls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryDetails_Accounts_CreatedBy",
                table: "SalaryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryDetails_Payrolls_PayrollId",
                table: "SalaryDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalaryDetails_CreatedBy",
                table: "SalaryDetails");

            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "OrderDetails");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "Notifications");

            migrationBuilder.AlterColumn<int>(
                name: "BaseWorkDay",
                table: "Contracts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryDetails_Payrolls_PayrollId",
                table: "SalaryDetails",
                column: "PayrollId",
                principalTable: "Payrolls",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
