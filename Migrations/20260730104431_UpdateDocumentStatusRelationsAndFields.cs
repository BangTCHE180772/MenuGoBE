using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentStatusRelationsAndFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentPartners_DocumentId",
                table: "DocumentPartners");

            // 1. Re-link any CashFlowDetails pointing to duplicate DocumentPartners to the kept (min Id) DocumentPartner row
            migrationBuilder.Sql(@"
                UPDATE ""CashFlowDetails"" cfd
                SET ""DocumentPartnerId"" = dup.min_id
                FROM ""DocumentPartners"" dp
                JOIN (
                    SELECT ""DocumentId"", MIN(""Id"") AS min_id
                    FROM ""DocumentPartners""
                    WHERE ""DocumentId"" IS NOT NULL
                    GROUP BY ""DocumentId""
                    HAVING COUNT(*) > 1
                ) dup ON dp.""DocumentId"" = dup.""DocumentId"" AND dp.""Id"" > dup.min_id
                WHERE cfd.""DocumentPartnerId"" = dp.""Id"";
            ");

            // 2. Safely delete duplicate DocumentPartners rows
            migrationBuilder.Sql(@"
                DELETE FROM ""DocumentPartners"" a
                USING ""DocumentPartners"" b
                WHERE a.""DocumentId"" = b.""DocumentId"" AND a.""Id"" > b.""Id"";
            ");

            // Add Status column with default 'Completed'
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Documents",
                type: "text",
                nullable: false,
                defaultValue: "Completed");

            // Migrate existing IsDeleted values to Status enum strings
            migrationBuilder.Sql(@"
                UPDATE ""Documents""
                SET ""Status"" = CASE WHEN ""IsDeleted"" = true THEN 'Cancelled' ELSE 'Completed' END;
            ");

            // Drop IsDeleted column
            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Documents");

            migrationBuilder.AddColumn<string>(
                name: "DeleteNote",
                table: "Documents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OtherCost",
                table: "Documents",
                type: "numeric(108,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "DocumentId",
                table: "CashFlows",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPartners_DocumentId",
                table: "DocumentPartners",
                column: "DocumentId",
                unique: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CashFlows_Documents_DocumentId",
                table: "CashFlows");

            migrationBuilder.DropIndex(
                name: "IX_DocumentPartners_DocumentId",
                table: "DocumentPartners");

            migrationBuilder.DropIndex(
                name: "IX_CashFlows_DocumentId",
                table: "CashFlows");

            migrationBuilder.DropColumn(
                name: "DeleteNote",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "OtherCost",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "CashFlows");

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Documents",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentPartners_DocumentId",
                table: "DocumentPartners",
                column: "DocumentId");
        }
    }
}
