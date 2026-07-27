using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class FixMissingDocumentColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""BranchId"" bigint NOT NULL DEFAULT 0;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""ParentDocumentId"" bigint NULL;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""BusinessDate"" timestamp with time zone NOT NULL DEFAULT now();
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""Type"" text NOT NULL DEFAULT 'Import';
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""Status"" text NOT NULL DEFAULT 'Draft';
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""TransferStatus"" text NOT NULL DEFAULT 'Pending';
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""TotalAmount"" numeric(30,12) NOT NULL DEFAULT 0;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""CreatedBy"" bigint NULL;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT now();
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""IsDeleted"" boolean NOT NULL DEFAULT false;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""DeletedBy"" bigint NULL;
                ALTER TABLE ""Documents"" ADD COLUMN IF NOT EXISTS ""DeletedAt"" timestamp with time zone NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse is not implemented to prevent accidental data loss.
        }
    }
}
