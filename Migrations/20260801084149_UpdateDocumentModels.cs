using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDocumentModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"InventoryLedgers\" DROP COLUMN IF EXISTS \"ActualQuantity\";");
            migrationBuilder.Sql("ALTER TABLE \"InventoryLedgers\" DROP COLUMN IF EXISTS \"RunningLeftOver\";");
            migrationBuilder.Sql("ALTER TABLE \"InventoryLedgers\" DROP COLUMN IF EXISTS \"VarianceQuantity\";");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" DROP COLUMN IF EXISTS \"Status\";");

            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='InventoryLedgers' AND column_name='BusinessDate') THEN ALTER TABLE \"InventoryLedgers\" RENAME COLUMN \"BusinessDate\" TO \"PostedAt\"; END IF; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Documents' AND column_name='BusinessDate') THEN ALTER TABLE \"Documents\" RENAME COLUMN \"BusinessDate\" TO \"OrderDate\"; END IF; END $$;");

            migrationBuilder.Sql("ALTER TABLE \"InventoryLedgers\" ADD COLUMN IF NOT EXISTS \"DocumentDetailId\" bigint NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE \"InventoryLedgers\" ADD COLUMN IF NOT EXISTS \"PostedBy\" bigint NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE \"InventoryLedgers\" ADD COLUMN IF NOT EXISTS \"SnapshotBranchName\" character varying(255) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE \"InventoryLedgers\" ADD COLUMN IF NOT EXISTS \"SnapshotPostedByName\" character varying(255) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE \"InventoryLedgers\" ADD COLUMN IF NOT EXISTS \"SnapshotProductName\" character varying(255) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE \"InventoryLedgers\" ADD COLUMN IF NOT EXISTS \"SnapshotUnitName\" character varying(100) NOT NULL DEFAULT '';");

            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"Note\" text NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"PostedAt\" timestamp with time zone NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"PostedBy\" bigint NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"SnapshotBranchName\" character varying(255) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"SnapshotCreatedByName\" character varying(255) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"SnapshotCreatedByUsername\" character varying(100) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"SnapshotDeletedByName\" character varying(255) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"SnapshotPartnerName\" character varying(255) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"SnapshotPostedByName\" character varying(255) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"SnapshotPostedByUsername\" character varying(100) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"SnapshotToBranchName\" character varying(255) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Documents\" ADD COLUMN IF NOT EXISTS \"ToBranchId\" bigint NULL;");

            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"ActualQuantity\" numeric(51,3) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"AdjustedCostDelta\" numeric(108,12) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"BaseQuantity\" numeric(51,3) NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"ConversionRate\" numeric(18,6) NOT NULL DEFAULT 1;");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"NewAvgCost\" numeric(108,12) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"SnapshotAvgCost\" numeric(108,12) NOT NULL DEFAULT 0;");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"SnapshotBaseUnitName\" character varying(100) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"SnapshotProductCode\" character varying(100) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"SnapshotProductName\" character varying(255) NOT NULL DEFAULT '';");
            migrationBuilder.Sql("ALTER TABLE \"DocumentDetails\" ADD COLUMN IF NOT EXISTS \"SnapshotUnitName\" character varying(100) NOT NULL DEFAULT '';");

            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_InventoryLedgers_DocumentDetailId\" ON \"InventoryLedgers\" (\"DocumentDetailId\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Documents_PostedBy\" ON \"Documents\" (\"PostedBy\");");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_Documents_ToBranchId\" ON \"Documents\" (\"ToBranchId\");");

            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Documents_Accounts_PostedBy') THEN ALTER TABLE \"Documents\" ADD CONSTRAINT \"FK_Documents_Accounts_PostedBy\" FOREIGN KEY (\"PostedBy\") REFERENCES \"Accounts\" (\"Id\") ON DELETE RESTRICT; END IF; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_Documents_Branches_ToBranchId') THEN ALTER TABLE \"Documents\" ADD CONSTRAINT \"FK_Documents_Branches_ToBranchId\" FOREIGN KEY (\"ToBranchId\") REFERENCES \"Branches\" (\"Id\") ON DELETE RESTRICT; END IF; END $$;");
            migrationBuilder.Sql("DO $$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_InventoryLedgers_DocumentDetails_DocumentDetailId') THEN ALTER TABLE \"InventoryLedgers\" ADD CONSTRAINT \"FK_InventoryLedgers_DocumentDetails_DocumentDetailId\" FOREIGN KEY (\"DocumentDetailId\") REFERENCES \"DocumentDetails\" (\"Id\") ON DELETE RESTRICT; END IF; END $$;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Accounts_PostedBy",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_Documents_Branches_ToBranchId",
                table: "Documents");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryLedgers_DocumentDetails_DocumentDetailId",
                table: "InventoryLedgers");

            migrationBuilder.DropIndex(
                name: "IX_InventoryLedgers_DocumentDetailId",
                table: "InventoryLedgers");

            migrationBuilder.DropIndex(
                name: "IX_Documents_PostedBy",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_ToBranchId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "DocumentDetailId",
                table: "InventoryLedgers");

            migrationBuilder.DropColumn(
                name: "PostedBy",
                table: "InventoryLedgers");

            migrationBuilder.DropColumn(
                name: "SnapshotBranchName",
                table: "InventoryLedgers");

            migrationBuilder.DropColumn(
                name: "SnapshotPostedByName",
                table: "InventoryLedgers");

            migrationBuilder.DropColumn(
                name: "SnapshotProductName",
                table: "InventoryLedgers");

            migrationBuilder.DropColumn(
                name: "SnapshotUnitName",
                table: "InventoryLedgers");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PostedBy",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotBranchName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotCreatedByName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotCreatedByUsername",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotDeletedByName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotPartnerName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotPostedByName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotPostedByUsername",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SnapshotToBranchName",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ToBranchId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ActualQuantity",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "AdjustedCostDelta",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "BaseQuantity",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "ConversionRate",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "NewAvgCost",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "SnapshotAvgCost",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "SnapshotBaseUnitName",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "SnapshotProductCode",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "SnapshotProductName",
                table: "DocumentDetails");

            migrationBuilder.DropColumn(
                name: "SnapshotUnitName",
                table: "DocumentDetails");

            migrationBuilder.RenameColumn(
                name: "PostedAt",
                table: "InventoryLedgers",
                newName: "BusinessDate");

            migrationBuilder.RenameColumn(
                name: "OrderDate",
                table: "Documents",
                newName: "BusinessDate");

            migrationBuilder.AlterColumn<long>(
                name: "DocumentId",
                table: "InventoryLedgers",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<decimal>(
                name: "ActualQuantity",
                table: "InventoryLedgers",
                type: "numeric(51,3)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RunningLeftOver",
                table: "InventoryLedgers",
                type: "numeric(108,12)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VarianceQuantity",
                table: "InventoryLedgers",
                type: "numeric(51,3)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "DocumentDetails",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
