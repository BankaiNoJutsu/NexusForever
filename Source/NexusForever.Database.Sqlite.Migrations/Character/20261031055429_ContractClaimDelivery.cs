using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Character
{
    /// <inheritdoc />
    public partial class ContractClaimDelivery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "characterCurrencyJson",
                table: "character_contract_reward_claim",
                type: "longtext",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "character_contract_account_outbox",
                columns: table => new
                {
                    claimId = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false),
                    characterId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    accountId = table.Column<uint>(type: "int(10)", nullable: false),
                    realmId = table.Column<ushort>(type: "smallint(5)", nullable: false),
                    trackId = table.Column<uint>(type: "int(10)", nullable: false),
                    cycleId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    ordinal = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    payloadJson = table.Column<string>(type: "longtext", nullable: false),
                    delivered = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.claimId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contract_outbox_character",
                table: "character_contract_account_outbox",
                columns: new[] { "characterId", "claimId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_contract_account_outbox");

            migrationBuilder.DropColumn(
                name: "characterCurrencyJson",
                table: "character_contract_reward_claim");
        }
    }
}
