using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddPayrollSuggestionsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PayrollSuggestions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BranchId = table.Column<long>(type: "bigint", nullable: false),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ApplyOption = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsResigned = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ManagerNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProcessedBy = table.Column<long>(type: "bigint", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsApplied = table.Column<bool>(type: "boolean", nullable: false),
                    AppliedPayrollId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollSuggestions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSuggestions_Accounts_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSuggestions_Accounts_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSuggestions_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollSuggestions_Payrolls_AppliedPayrollId",
                        column: x => x.AppliedPayrollId,
                        principalTable: "Payrolls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSuggestions_AccountId",
                table: "PayrollSuggestions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSuggestions_AppliedPayrollId",
                table: "PayrollSuggestions",
                column: "AppliedPayrollId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSuggestions_BranchId",
                table: "PayrollSuggestions",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSuggestions_CreatedBy",
                table: "PayrollSuggestions",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSuggestions_ProcessedBy",
                table: "PayrollSuggestions",
                column: "ProcessedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayrollSuggestions");
        }
    }
}
