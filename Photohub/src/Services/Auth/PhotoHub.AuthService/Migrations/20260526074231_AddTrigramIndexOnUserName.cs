using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoHub.AuthService.Migrations
{
    /// <inheritdoc />
    public partial class AddTrigramIndexOnUserName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(@"
                CREATE INDEX IX_users_UserName_Trgm
                ON ""users""
                USING gin (""UserName"" gin_trgm_ops);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""IX_users_UserName_Trgm"";");
            migrationBuilder.Sql("DROP EXTENSION IF EXISTS pg_trgm;");
        }
    }
}
