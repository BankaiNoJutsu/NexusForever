using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Character
{
    /// <inheritdoc />
    public partial class ContractRewardSelections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "selectionInitialized",
                table: "character_contract_reward_track",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "character_contract_reward_slot",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    trackId = table.Column<uint>(type: "int(10)", nullable: false),
                    cycleId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    ordinal = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    rewardRowId = table.Column<uint>(type: "int(10)", nullable: false),
                    requiredPoints = table.Column<uint>(type: "int(10)", nullable: false),
                    selectionJson = table.Column<string>(type: "longtext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.trackId, x.cycleId, x.ordinal });
                    table.ForeignKey(
                        name: "FK_contract_reward_slot_track",
                        columns: x => new { x.id, x.trackId },
                        principalTable: "character_contract_reward_track",
                        principalColumns: new[] { "id", "trackId" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_contract_reward_slot");

            migrationBuilder.DropColumn(
                name: "selectionInitialized",
                table: "character_contract_reward_track");
        }
    }
}
