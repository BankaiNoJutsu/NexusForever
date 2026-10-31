using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Auth
{
    /// <inheritdoc />
    public partial class AccountCurrencyOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account_currency_operation",
                columns: table => new
                {
                    operationKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    accountId = table.Column<uint>(type: "int(10)", nullable: false),
                    currencyId = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    amount = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    subtract = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    beforeAmount = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    afterAmount = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    sourceJson = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.operationKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_currency_operation_owner",
                table: "account_currency_operation",
                columns: new[] { "accountId", "currencyId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_currency_operation");
        }
    }
}
