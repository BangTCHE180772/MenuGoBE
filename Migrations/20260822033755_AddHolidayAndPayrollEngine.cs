using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidayAndPayrollEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryDetails_Payrolls_PayrollId",
                table: "SalaryDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "ApprovedOTHours",
                table: "WorkSchedules",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoCheckout",
                table: "WorkSchedules",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "OTNotes",
                table: "WorkSchedules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OTStatus",
                table: "WorkSchedules",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "StandardHours",
                table: "WorkSchedules",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsNightShift",
                table: "Shifts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "NightAllowance",
                table: "Shifts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NightBonusRate",
                table: "Shifts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "StandardHours",
                table: "Shifts",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Payrolls",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ApprovedBy",
                table: "Payrolls",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HourlyRate",
                table: "Payrolls",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "Payrolls",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LockedBy",
                table: "Payrolls",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalBaseShiftPay",
                table: "Payrolls",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalHolidayBonus",
                table: "Payrolls",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalNightPay",
                table: "Payrolls",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalOTPay",
                table: "Payrolls",
                type: "numeric(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<int>(
                name: "BaseWorkDay",
                table: "Contracts",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.CreateTable(
                name: "HolidayConfigs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "text", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Coefficient = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HolidayConfigs_Accounts_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HolidayConfigs_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollShiftDetails",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PayrollId = table.Column<long>(type: "bigint", nullable: false),
                    WorkScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualHours = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    StandardHours = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    ApprovedOTHours = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DateCoefficient = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    IsNightShift = table.Column<bool>(type: "boolean", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    BaseShiftPay = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    NightShiftPay = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    OTPay = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    TotalShiftPay = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollShiftDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollShiftDetails_Payrolls_PayrollId",
                        column: x => x.PayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollShiftDetails_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });



            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_ApprovedBy",
                table: "Payrolls",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Payrolls_LockedBy",
                table: "Payrolls",
                column: "LockedBy");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayConfigs_BranchId",
                table: "HolidayConfigs",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_HolidayConfigs_CreatedBy",
                table: "HolidayConfigs",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollShiftDetails_PayrollId",
                table: "PayrollShiftDetails",
                column: "PayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollShiftDetails_WorkScheduleId",
                table: "PayrollShiftDetails",
                column: "WorkScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payrolls_Accounts_ApprovedBy",
                table: "Payrolls",
                column: "ApprovedBy",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payrolls_Accounts_LockedBy",
                table: "Payrolls",
                column: "LockedBy",
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
                name: "FK_Payrolls_Accounts_ApprovedBy",
                table: "Payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_Payrolls_Accounts_LockedBy",
                table: "Payrolls");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryDetails_Accounts_CreatedBy",
                table: "SalaryDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryDetails_Payrolls_PayrollId",
                table: "SalaryDetails");

            migrationBuilder.DropTable(
                name: "HolidayConfigs");

            migrationBuilder.DropTable(
                name: "PayrollShiftDetails");

            migrationBuilder.DropIndex(
                name: "IX_SalaryDetails_CreatedBy",
                table: "SalaryDetails");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_ApprovedBy",
                table: "Payrolls");

            migrationBuilder.DropIndex(
                name: "IX_Payrolls_LockedBy",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ApprovedOTHours",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "IsAutoCheckout",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "OTNotes",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "OTStatus",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "StandardHours",
                table: "WorkSchedules");

            migrationBuilder.DropColumn(
                name: "IsNightShift",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "NightAllowance",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "NightBonusRate",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "StandardHours",
                table: "Shifts");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "HourlyRate",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "LockedBy",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "TotalBaseShiftPay",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "TotalHolidayBonus",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "TotalNightPay",
                table: "Payrolls");

            migrationBuilder.DropColumn(
                name: "TotalOTPay",
                table: "Payrolls");

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
