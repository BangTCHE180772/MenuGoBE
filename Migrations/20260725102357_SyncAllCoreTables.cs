using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class SyncAllCoreTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                -- Branches
                ALTER TABLE ""Branches"" ADD COLUMN IF NOT EXISTS ""ChainId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""Branches"" ADD COLUMN IF NOT EXISTS ""AddressId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""Branches"" ADD COLUMN IF NOT EXISTS ""Name"" text NOT NULL DEFAULT '';
                ALTER TABLE ""Branches"" ADD COLUMN IF NOT EXISTS ""Type"" text NOT NULL DEFAULT '';
                ALTER TABLE ""Branches"" ADD COLUMN IF NOT EXISTS ""CloseTime"" time without time zone NOT NULL DEFAULT '00:00:00';
                ALTER TABLE ""Branches"" ADD COLUMN IF NOT EXISTS ""OpenTime"" time without time zone NOT NULL DEFAULT '00:00:00';
                ALTER TABLE ""Branches"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now();
                ALTER TABLE ""Branches"" ADD COLUMN IF NOT EXISTS ""PostingLockDate"" timestamp with time zone NULL;

                -- BInventories
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""BranchId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""ProductId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""Type"" text NOT NULL DEFAULT 'Ingredient';
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""Avg"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""LeftOver"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""Quantity"" numeric(21,3) NOT NULL DEFAULT 0;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""ChainActive"" boolean NOT NULL DEFAULT false;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""BranchActive"" boolean NOT NULL DEFAULT false;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""IsManageQuantity"" boolean NOT NULL DEFAULT false;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""MinStorage"" numeric(18,3) NOT NULL DEFAULT 0;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""MaxStorage"" numeric(18,3) NOT NULL DEFAULT 1000000000;
                ALTER TABLE ""BInventories"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now();

                -- InventoryLedgers
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""BInventoryId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""DocumentId"" bigint NULL;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""DocumentDetailId"" bigint NULL;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""BusinessDate"" timestamp with time zone NOT NULL DEFAULT now();
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""DocumentType"" text NOT NULL DEFAULT 'Import';
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""QuantityDelta"" numeric(21,3) NOT NULL DEFAULT 0;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""InventoryValueDelta"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""RunningQuantity"" numeric(21,3) NOT NULL DEFAULT 0;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""RunningInventoryValue"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""RunningAverageCost"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""RunningLeftOver"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""ActualQuantity"" numeric(21,3) NULL;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""VarianceQuantity"" numeric(21,3) NULL;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""UnitCost"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now();
                ALTER TABLE ""InventoryLedgers"" ADD COLUMN IF NOT EXISTS ""CreatedBy"" bigint NULL;

                -- DocumentDetails
                ALTER TABLE ""DocumentDetails"" ADD COLUMN IF NOT EXISTS ""DocumentId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""DocumentDetails"" ADD COLUMN IF NOT EXISTS ""BInventoryId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""DocumentDetails"" ADD COLUMN IF NOT EXISTS ""Quantity"" numeric(21,3) NOT NULL DEFAULT 0;
                ALTER TABLE ""DocumentDetails"" ADD COLUMN IF NOT EXISTS ""SystemQuantity"" numeric(21,3) NULL;
                ALTER TABLE ""DocumentDetails"" ADD COLUMN IF NOT EXISTS ""UnitPrice"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""DocumentDetails"" ADD COLUMN IF NOT EXISTS ""Note"" text NULL;
                ALTER TABLE ""DocumentDetails"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now();

                -- CashFlows
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""BranchId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""Code"" character varying(100) NOT NULL DEFAULT '';
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""BusinessDate"" timestamp with time zone NOT NULL DEFAULT now();
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""Direction"" text NOT NULL DEFAULT 'In';
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""Status"" text NOT NULL DEFAULT 'Draft';
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""TotalAmount"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now();
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""IsDeleted"" boolean NOT NULL DEFAULT false;
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""DeletedBy"" bigint NULL;
                ALTER TABLE ""CashFlows"" ADD COLUMN IF NOT EXISTS ""DeletedAt"" timestamp with time zone NULL;

                -- CashFlowDetails
                ALTER TABLE ""CashFlowDetails"" ADD COLUMN IF NOT EXISTS ""CashFlowId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""CashFlowDetails"" ADD COLUMN IF NOT EXISTS ""DocumentPartnerId"" bigint NULL;
                ALTER TABLE ""CashFlowDetails"" ADD COLUMN IF NOT EXISTS ""PartnerId"" bigint NULL;
                ALTER TABLE ""CashFlowDetails"" ADD COLUMN IF NOT EXISTS ""Type"" text NOT NULL DEFAULT 'Payment';
                ALTER TABLE ""CashFlowDetails"" ADD COLUMN IF NOT EXISTS ""PaymentMethod"" text NOT NULL DEFAULT 'Cash';
                ALTER TABLE ""CashFlowDetails"" ADD COLUMN IF NOT EXISTS ""Amount"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""CashFlowDetails"" ADD COLUMN IF NOT EXISTS ""Note"" text NULL;

                -- Partners
                ALTER TABLE ""Partners"" ADD COLUMN IF NOT EXISTS ""BranchId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""Partners"" ADD COLUMN IF NOT EXISTS ""Type"" text NOT NULL DEFAULT 'Supplier';
                ALTER TABLE ""Partners"" ADD COLUMN IF NOT EXISTS ""Name"" character varying(255) NOT NULL DEFAULT '';
                ALTER TABLE ""Partners"" ADD COLUMN IF NOT EXISTS ""AddressId"" bigint NULL;
                ALTER TABLE ""Partners"" ADD COLUMN IF NOT EXISTS ""Phone"" character varying(50) NULL;
                ALTER TABLE ""Partners"" ADD COLUMN IF NOT EXISTS ""Email"" character varying(255) NULL;
                ALTER TABLE ""Partners"" ADD COLUMN IF NOT EXISTS ""CreatedBy"" bigint NULL;
                ALTER TABLE ""Partners"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now();
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
