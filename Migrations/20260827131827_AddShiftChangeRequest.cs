using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftChangeRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShiftChangeRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkScheduleId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OldShiftId = table.Column<long>(type: "bigint", nullable: false),
                    OldStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    OldEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    OldStatus = table.Column<string>(type: "text", nullable: false),
                    OldManagerApproved = table.Column<bool>(type: "boolean", nullable: false),
                    NewShiftId = table.Column<long>(type: "bigint", nullable: false),
                    NewStartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    NewEndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    IsNightShift = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectReason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Accounts_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Shifts_NewShiftId",
                        column: x => x.NewShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_Shifts_OldShiftId",
                        column: x => x.OldShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftChangeRequests_WorkSchedules_WorkScheduleId",
                        column: x => x.WorkScheduleId,
                        principalTable: "WorkSchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_AccountId",
                table: "ShiftChangeRequests",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_ApprovedBy",
                table: "ShiftChangeRequests",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_BranchId",
                table: "ShiftChangeRequests",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_NewShiftId",
                table: "ShiftChangeRequests",
                column: "NewShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_OldShiftId",
                table: "ShiftChangeRequests",
                column: "OldShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftChangeRequests_WorkScheduleId",
                table: "ShiftChangeRequests",
                column: "WorkScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShiftChangeRequests");
        }
    }
}
