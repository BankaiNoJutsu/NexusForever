using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
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
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    trackId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    cycleId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false),
                    ordinal = table.Column<byte>(type: "tinyint(3) unsigned", nullable: false),
                    rewardRowId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    choice = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    claimId = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    accountId = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    quoteVersion = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    quoteJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    deliveryVersion = table.Column<uint>(type: "int(10) unsigned", nullable: false),
                    deliveryJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                })
                .Annotation("MySql:CharSet", "utf8mb4");

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
