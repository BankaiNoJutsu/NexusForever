using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class MarketplacePersistence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "marketplace_auction",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    ownerCharacterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    itemId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    minimumBid = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    buyoutPrice = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    currentBid = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    topBidderCharacterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    expirationTime = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    item2Id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    quantity = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    worldRequirementItem2Id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    circuitData = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    glyphData = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    thresholdData = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    unknown2 = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    unknownArray = table.Column<string>(type: "varchar(255)", nullable: true, defaultValue: "")
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__marketplace_auction_itemId__item_id",
                        column: x => x.itemId,
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "marketplace_commodity_order",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    ownerCharacterId = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    item2Id = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    quantity = table.Column<uint>(type: "int(10) unsigned", nullable: false, defaultValue: 0u),
                    pricePerUnit = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    price = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    isBuyOrder = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    forceImmediate = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    listTime = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul),
                    expirationTime = table.Column<ulong>(type: "bigint(20) unsigned", nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_auction_itemId",
                table: "marketplace_auction",
                column: "itemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marketplace_auction");

            migrationBuilder.DropTable(
                name: "marketplace_commodity_order");
        }
    }
}
