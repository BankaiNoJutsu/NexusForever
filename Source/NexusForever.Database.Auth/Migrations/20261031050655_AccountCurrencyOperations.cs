using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
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
                    operationKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    currencyId = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    amount = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    subtract = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    beforeAmount = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    afterAmount = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    sourceJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.operationKey);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
