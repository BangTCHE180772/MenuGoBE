using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusToDocumentDetailAndSimplifyDocumentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLedgers_Accounts_CreatedBy",
                table: "InventoryLedgers");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLedgers_CreatedBy",
                table: "InventoryLedgers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "InventoryLedgers");

            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Products",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStorage",
                table: "Products",
                type: "numeric(51,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxStorage",
                table: "Products",
                type: "numeric(51,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "VarianceQuantity",
                table: "InventoryLedgers",
                type: "numeric(51,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(21,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "InventoryLedgers",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RunningQuantity",
                table: "InventoryLedgers",
                type: "numeric(51,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(21,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RunningLeftOver",
                table: "InventoryLedgers",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RunningInventoryValue",
                table: "InventoryLedgers",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RunningAverageCost",
                table: "InventoryLedgers",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityDelta",
                table: "InventoryLedgers",
                type: "numeric(51,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(21,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "InventoryValueDelta",
                table: "InventoryLedgers",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualQuantity",
                table: "InventoryLedgers",
                type: "numeric(51,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(21,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Documents",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Cost",
                table: "DocumentPartners",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "DocumentDetails",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "SystemQuantity",
                table: "DocumentDetails",
                type: "numeric(51,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(21,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "DocumentDetails",
                type: "numeric(51,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(21,3)");

            migrationBuilder.AddColumn<long>(
                name: "FatherId",
                table: "DocumentDetails",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "DocumentDetails",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "UnitConversionId",
                table: "DocumentDetails",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "CashFlows",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CashFlowDetails",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "BInventories",
                type: "numeric(51,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(21,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStorage",
                table: "BInventories",
                type: "numeric(51,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxStorage",
                table: "BInventories",
                type: "numeric(51,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "LeftOver",
                table: "BInventories",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Avg",
                table: "BInventories",
                type: "numeric(108,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(30,12)");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""Payrolls"" (
                    ""Id"" bigint GENERATED BY DEFAULT AS IDENTITY,
                    ""AccountId"" bigint NOT NULL,
                    ""BranchId"" bigint NOT NULL,
                    ""ContractId"" bigint,
                    ""Month"" integer NOT NULL,
                    ""Year"" integer NOT NULL,
                    ""SalaryType"" text NOT NULL,
                    ""BaseSalary"" numeric(18,4) NOT NULL,
                    ""BaseWorkDays"" integer NOT NULL,
                    ""ActualWorkDays"" numeric(18,4) NOT NULL,
                    ""ActualWorkHours"" numeric(18,4) NOT NULL,
                    ""CalculatedSalary"" numeric(18,4) NOT NULL,
                    ""TotalAllowance"" numeric(18,4) NOT NULL,
                    ""BonusAmount"" numeric(18,4) NOT NULL,
                    ""TotalDeduction"" numeric(18,4) NOT NULL,
                    ""PenaltyAmount"" numeric(18,4) NOT NULL,
                    ""NetSalary"" numeric(18,4) NOT NULL,
                    ""Status"" text NOT NULL,
                    ""PaymentDate"" timestamp with time zone,
                    ""Note"" text NOT NULL,
                    ""CreatedBy"" bigint NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_Payrolls"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_Payrolls_Accounts_AccountId"" FOREIGN KEY (""AccountId"") REFERENCES ""Accounts"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_Payrolls_Branches_BranchId"" FOREIGN KEY (""BranchId"") REFERENCES ""Branches"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_Payrolls_Contracts_ContractId"" FOREIGN KEY (""ContractId"") REFERENCES ""Contracts"" (""Id"") ON DELETE RESTRICT
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""SalaryDetails"" (
                    ""Id"" bigint GENERATED BY DEFAULT AS IDENTITY,
                    ""PayrollId"" bigint NOT NULL,
                    ""AccountId"" bigint NOT NULL,
                    ""Title"" text NOT NULL,
                    ""Type"" text NOT NULL,
                    ""Amount"" numeric(18,4) NOT NULL,
                    ""Note"" text NOT NULL,
                    ""CreatedBy"" bigint NOT NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    CONSTRAINT ""PK_SalaryDetails"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_SalaryDetails_Accounts_AccountId"" FOREIGN KEY (""AccountId"") REFERENCES ""Accounts"" (""Id"") ON DELETE RESTRICT,
                    CONSTRAINT ""FK_SalaryDetails_Payrolls_PayrollId"" FOREIGN KEY (""PayrollId"") REFERENCES ""Payrolls"" (""Id"") ON DELETE RESTRICT
                );
            ");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDetails_FatherId",
                table: "DocumentDetails",
                column: "FatherId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentDetails_UnitConversionId",
                table: "DocumentDetails",
                column: "UnitConversionId");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Payrolls_AccountId_Month_Year"" ON ""Payrolls"" (""AccountId"", ""Month"", ""Year"");
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Payrolls_BranchId"" ON ""Payrolls"" (""BranchId"");
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_Payrolls_ContractId"" ON ""Payrolls"" (""ContractId"");
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_SalaryDetails_AccountId"" ON ""SalaryDetails"" (""AccountId"");
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_SalaryDetails_PayrollId"" ON ""SalaryDetails"" (""PayrollId"");
            ");

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentDetails_DocumentDetails_FatherId",
                table: "DocumentDetails",
                column: "FatherId",
                principalTable: "DocumentDetails",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentDetails_UnitConversions_UnitConversionId",
                table: "DocumentDetails",
                column: "UnitConversionId",
                principalTable: "UnitConversions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentDetails_DocumentDetails_FatherId",
                table: "DocumentDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentDetails_UnitConversions_UnitConversionId",
                table: "DocumentDetails");

            migrationBuilder.DropTable(
                name: "SalaryDetails");

            migrationBuilder.DropTable(
                name: "Payrolls");

            migrationBuilder.DropIndex(
                name: "IX_DocumentDetails_FatherId",
                table: "DocumentDetails");

            migrationBuilder.DropIndex(
                name: "IX_DocumentDetails_UnitConversionId",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "FatherId",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "UnitConversionId",
                table: "DocumentDetails");

            migrationBuilder.AlterColumn<decimal>(
                name: "SellPrice",
                table: "Products",
                type: "numeric(18,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStorage",
                table: "Products",
                type: "numeric(18,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxStorage",
                table: "Products",
                type: "numeric(18,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "VarianceQuantity",
                table: "InventoryLedgers",
                type: "numeric(21,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitCost",
                table: "InventoryLedgers",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RunningQuantity",
                table: "InventoryLedgers",
                type: "numeric(21,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RunningLeftOver",
                table: "InventoryLedgers",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RunningInventoryValue",
                table: "InventoryLedgers",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "RunningAverageCost",
                table: "InventoryLedgers",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "QuantityDelta",
                table: "InventoryLedgers",
                type: "numeric(21,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "InventoryValueDelta",
                table: "InventoryLedgers",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "ActualQuantity",
                table: "InventoryLedgers",
                type: "numeric(21,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)",
                oldNullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CreatedBy",
                table: "InventoryLedgers",
                type: "bigint",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "Documents",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Cost",
                table: "DocumentPartners",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UnitPrice",
                table: "DocumentDetails",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "SystemQuantity",
                table: "DocumentDetails",
                type: "numeric(21,3)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "DocumentDetails",
                type: "numeric(21,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "TotalAmount",
                table: "CashFlows",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Amount",
                table: "CashFlowDetails",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "BInventories",
                type: "numeric(21,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStorage",
                table: "BInventories",
                type: "numeric(18,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "MaxStorage",
                table: "BInventories",
                type: "numeric(18,3)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(51,3)");

            migrationBuilder.AlterColumn<decimal>(
                name: "LeftOver",
                table: "BInventories",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Avg",
                table: "BInventories",
                type: "numeric(30,12)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(108,12)");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryLedgers_CreatedBy",
                table: "InventoryLedgers",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryLedgers_Accounts_CreatedBy",
                table: "InventoryLedgers",
                column: "CreatedBy",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
