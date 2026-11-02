using Microsoft.EntityFrameworkCore.Migrations;

using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    /// <inheritdoc />
    public partial class RepairCreddHistoryIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL otherwise renumbers a retained zero key while adding identity.
            // The migrator owns this connection; restore its mode on successful DDL.
            migrationBuilder.Sql("SET @nexus_credd_history_sql_mode = @@SESSION.sql_mode;");
            migrationBuilder.Sql("SET SESSION sql_mode = CONCAT_WS(',', NULLIF(@nexus_credd_history_sql_mode, ''), 'NO_AUTO_VALUE_ON_ZERO');");
            migrationBuilder.AlterColumn<ulong>(
                name: "id", table: "account_credd_history", type: "bigint(20) unsigned", nullable: false,
                oldClrType: typeof(ulong), oldType: "bigint(20) unsigned")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
            migrationBuilder.Sql("SET SESSION sql_mode = @nexus_credd_history_sql_mode;");
            migrationBuilder.Sql("SET @nexus_credd_history_sql_mode = NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ulong>(
                name: "id", table: "account_credd_history", type: "bigint(20) unsigned", nullable: false,
                oldClrType: typeof(ulong), oldType: "bigint(20) unsigned")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }
    }
}
