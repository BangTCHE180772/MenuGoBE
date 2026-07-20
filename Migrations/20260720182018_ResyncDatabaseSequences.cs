using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MenuGoBE.Migrations
{
    /// <inheritdoc />
    public partial class ResyncDatabaseSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    r RECORD;
                    seq TEXT;
                BEGIN
                    FOR r IN (
                        SELECT table_name, column_name
                        FROM information_schema.columns
                        WHERE table_schema = 'public'
                          AND column_name = 'Id'
                          AND data_type IN ('bigint', 'integer')
                    ) LOOP
                        BEGIN
                            seq := pg_get_serial_sequence('public.' || quote_ident(r.table_name), r.column_name);
                            IF seq IS NOT NULL THEN
                                EXECUTE format('SELECT setval(%L, COALESCE(max(%I), 0) + 1, false) FROM %I',
                                               seq, r.column_name, r.table_name);
                            END IF;
                        EXCEPTION WHEN OTHERS THEN
                            -- Ignore errors gracefully
                        END;
                    END LOOP;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
