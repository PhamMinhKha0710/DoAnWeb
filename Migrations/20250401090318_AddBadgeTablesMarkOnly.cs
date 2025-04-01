using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeTablesMarkOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The Badge and BadgeAssignment tables were manually created in the database
            // This migration only marks them as applied in the EF Core migration history
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Badges]') AND type in (N'U'))
                BEGIN
                    PRINT 'Warning: Badges table does not exist. This migration assumes it was already created.';
                END
                
                IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BadgeAssignments]') AND type in (N'U'))
                BEGIN
                    PRINT 'Warning: BadgeAssignments table does not exist. This migration assumes it was already created.';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // This is a marking migration only, we don't want to drop the tables in Down()
            // since they were manually created and might contain valuable data
        }
    }
}
