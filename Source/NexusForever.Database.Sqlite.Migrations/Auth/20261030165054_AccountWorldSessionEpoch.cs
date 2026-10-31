using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Auth
{
    /// <inheritdoc />
    public partial class AccountWorldSessionEpoch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_world_session",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "int(10)", nullable: false),
                    epoch = table.Column<ulong>(type: "bigint(20)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.accountId);
                    table.ForeignKey(
                        name: "FK__account_world_session_accountId__account_id",
                        column: x => x.accountId,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_world_session");
        }
    }
}
