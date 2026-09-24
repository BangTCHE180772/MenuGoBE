using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToCoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Commented out to fix "column already exists" error during migration
            // migrationBuilder.AddColumn<DateTime>(
            //     name: "DeletedAt",
            //     table: "Documents",
            //     type: "timestamp with time zone",
            //     nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Documents",
                type: "boolean",
                defaultValue: false);

            // Commented out to fix "column already exists" error during migration
            // migrationBuilder.AddColumn<long?>(
            //     name: "DeletedBy",
            //     table: "Documents",
            //     type: "bigint",
            //     nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "DocumentDetails",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // migrationBuilder.CreateIndex(
            //     name: "IX_Documents_DeletedBy",
            //     table: "Documents",
            //     column: "DeletedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Documents_DeletedBy",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "DocumentDetails");
        }
    }
}
