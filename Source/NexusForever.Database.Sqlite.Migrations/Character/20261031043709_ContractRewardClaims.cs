using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Character
{
    /// <inheritdoc />
    public partial class ContractRewardClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "claimed",
                table: "character_contract_reward_slot",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "character_contract_reward_claim",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    trackId = table.Column<uint>(type: "int(10)", nullable: false),
                    cycleId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    ordinal = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    rewardRowId = table.Column<uint>(type: "int(10)", nullable: false),
                    choice = table.Column<uint>(type: "int(10)", nullable: false),
                    claimId = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false),
                    accountId = table.Column<uint>(type: "int(10)", nullable: false),
                    quoteVersion = table.Column<uint>(type: "int(10)", nullable: false),
                    quoteJson = table.Column<string>(type: "longtext", nullable: false),
                    deliveryVersion = table.Column<uint>(type: "int(10)", nullable: false),
                    deliveryJson = table.Column<string>(type: "longtext", nullable: false),
                    createdUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.trackId, x.cycleId, x.ordinal });
                    table.ForeignKey(
                        name: "FK_contract_reward_claim_character",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contract_reward_claim_id",
                table: "character_contract_reward_claim",
                column: "claimId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_contract_reward_claim");

            migrationBuilder.DropColumn(
                name: "claimed",
                table: "character_contract_reward_slot");
        }
    }
}
