using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.World
{
    /// <inheritdoc />
    public partial class WorldSqliteInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "creature_info_property",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10)", nullable: false),
                    property = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    value = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.property });
                });

            migrationBuilder.CreateTable(
                name: "creature_info_stat",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10)", nullable: false),
                    stat = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    value = table.Column<float>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.stat });
                });

            migrationBuilder.CreateTable(
                name: "creature_loot",
                columns: table => new
                {
                    creatureId = table.Column<uint>(type: "int(10)", nullable: false),
                    itemId = table.Column<uint>(type: "int(10)", nullable: false),
                    chance = table.Column<decimal>(type: "decimal(12,8)", nullable: false, defaultValue: 0m),
                    dropTimes = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    aggregateDropSum = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    aggregateDropCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    gameVersion = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    sourceDropId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    versionedItemDropAggregateId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    versionedCreatureDropAggregateId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    lastSeenIn = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    matchStatus = table.Column<string>(type: "varchar(32)", nullable: false, defaultValue: ""),
                    sourceName = table.Column<string>(type: "varchar(255)", nullable: false, defaultValue: ""),
                    itemName = table.Column<string>(type: "varchar(255)", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.creatureId, x.itemId });
                });

            migrationBuilder.CreateTable(
                name: "disable",
                columns: table => new
                {
                    type = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    objectId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    note = table.Column<string>(type: "varchar(500)", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.type, x.objectId });
                });

            migrationBuilder.CreateTable(
                name: "entity",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                        .Annotation("Sqlite:Autoincrement", true),
                    type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    creature = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    world = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    area = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    x = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    y = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    z = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    rx = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    ry = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    rz = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    displayInfo = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    outfitInfo = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    faction1 = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    faction2 = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    questChecklistIdx = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    mode = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    activePropId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    worldSocketId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "item_salvage",
                columns: table => new
                {
                    purpose = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    sourceItemId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    sourceItem2TypeId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    sourceLevel = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    type = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    staticId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    probability = table.Column<float>(type: "float", nullable: false, defaultValue: 100f),
                    minCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    maxCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    comment = table.Column<string>(type: "varchar(200)", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.purpose, x.sourceItemId, x.sourceItem2TypeId, x.sourceLevel, x.type, x.staticId });
                });

            migrationBuilder.CreateTable(
                name: "loot_group",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    parentId = table.Column<ulong>(type: "bigint(20)", nullable: true),
                    probability = table.Column<float>(type: "float", nullable: false, defaultValue: 100f),
                    minDrop = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    maxDrop = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    conditionType = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    condition = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    comment = table.Column<string>(type: "varchar(200)", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loot_group", x => x.id);
                    table.ForeignKey(
                        name: "FK__loot_group_parentId__loot_group_id",
                        column: x => x.parentId,
                        principalTable: "loot_group",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "map_entrance",
                columns: table => new
                {
                    mapId = table.Column<ushort>(type: "int(10)", nullable: false),
                    team = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    worldLocationId = table.Column<uint>(type: "int(10)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.mapId, x.team });
                });

            migrationBuilder.CreateTable(
                name: "store_category",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                        .Annotation("Sqlite:Autoincrement", true),
                    parentId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 26u),
                    name = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: ""),
                    description = table.Column<string>(type: "varchar(150)", nullable: false, defaultValue: ""),
                    index = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 1u),
                    visible = table.Column<byte>(type: "tinyint(1)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_category", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "store_offer_group",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                        .Annotation("Sqlite:Autoincrement", true),
                    displayFlags = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    name = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: ""),
                    description = table.Column<string>(type: "varchar(500)", nullable: false, defaultValue: ""),
                    displayInfoOverride = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    visible = table.Column<byte>(type: "tinyint(1)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_offer_group", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tutorial",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u, comment: "Tutorial ID"),
                    type = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    triggerId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    note = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.type, x.triggerId });
                });

            migrationBuilder.CreateTable(
                name: "version",
                columns: table => new
                {
                    fileName = table.Column<string>(type: "TEXT", nullable: false),
                    fileHash = table.Column<string>(type: "TEXT", nullable: false),
                    appliedOn = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.fileName, x.fileHash });
                });

            migrationBuilder.CreateTable(
                name: "entity_event",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    eventId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    phase = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.eventId, x.phase });
                    table.ForeignKey(
                        name: "FK__entity_event_id__entity_id",
                        column: x => x.id,
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_property",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    property = table.Column<byte>(type: "tinyint(3)", nullable: false),
                    value = table.Column<float>(type: "float", nullable: false, defaultValue: 0f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.property });
                    table.ForeignKey(
                        name: "FK__entity_property_id__entity_id",
                        column: x => x.id,
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_script",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    scriptName = table.Column<string>(type: "varchar(150)", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.scriptName });
                    table.ForeignKey(
                        name: "FK__entity_script_id__entity_id",
                        column: x => x.id,
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_spline",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                        .Annotation("Sqlite:Autoincrement", true),
                    splineId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    mode = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    speed = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    fx = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    fy = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    fz = table.Column<float>(type: "float", nullable: false, defaultValue: 0f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_spline", x => x.id);
                    table.ForeignKey(
                        name: "FK__entity_spline_id__entity_id",
                        column: x => x.id,
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_stats",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    stat = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    value = table.Column<float>(type: "float", nullable: false, defaultValue: 0f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.stat });
                    table.ForeignKey(
                        name: "FK__entity_stats_stat_id_entity_id",
                        column: x => x.id,
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_vendor",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                        .Annotation("Sqlite:Autoincrement", true),
                    buyPriceMultiplier = table.Column<float>(type: "float", nullable: false, defaultValue: 1f),
                    sellPriceMultiplier = table.Column<float>(type: "float", nullable: false, defaultValue: 1f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_vendor", x => x.id);
                    table.ForeignKey(
                        name: "FK__entity_vendor_id__entity_id",
                        column: x => x.id,
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_vendor_category",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    index = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    localisedTextId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.index });
                    table.ForeignKey(
                        name: "FK__entity_vendor_category_id__entity_id",
                        column: x => x.id,
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_vendor_item",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    index = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    categoryIndex = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    itemId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    extraCost1Type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    extraCost1Quantity = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    extraCost1ItemOrCurrencyId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    extraCost2Type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    extraCost2Quantity = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    extraCost2ItemOrCurrencyId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.index });
                    table.ForeignKey(
                        name: "FK__entity_vendor_item_id__entity_id",
                        column: x => x.id,
                        principalTable: "entity",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "entity_loot",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    lootGroupId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    comment = table.Column<string>(type: "varchar(200)", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.lootGroupId });
                    table.ForeignKey(
                        name: "FK_entity_loot_loot_group_lootGroupId",
                        column: x => x.lootGroupId,
                        principalTable: "loot_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item_loot",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    lootGroupId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    comment = table.Column<string>(type: "varchar(200)", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.lootGroupId });
                    table.ForeignKey(
                        name: "FK_item_loot_loot_group_lootGroupId",
                        column: x => x.lootGroupId,
                        principalTable: "loot_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "loot_item",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    type = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    staticId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    probability = table.Column<float>(type: "float", nullable: false, defaultValue: 100f),
                    minCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    maxCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    comment = table.Column<string>(type: "varchar(200)", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.type, x.staticId });
                    table.ForeignKey(
                        name: "FK__loot_item_id__loot_group_id",
                        column: x => x.id,
                        principalTable: "loot_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_offer_group_category",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    categoryId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    index = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    visible = table.Column<byte>(type: "tinyint(1)", nullable: false, defaultValue: (byte)1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.categoryId });
                    table.ForeignKey(
                        name: "FK__store_offer_group_category_categoryId__store_category_id",
                        column: x => x.categoryId,
                        principalTable: "store_category",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__store_offer_group_category_id__store_offer_group_id",
                        column: x => x.id,
                        principalTable: "store_offer_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_offer_item",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    groupId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    name = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: ""),
                    description = table.Column<string>(type: "varchar(500)", nullable: false, defaultValue: ""),
                    displayFlags = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    field_6 = table.Column<long>(type: "bigint(20)", nullable: false, defaultValue: 0L),
                    field_7 = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    visible = table.Column<byte>(type: "tinyint(1)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.groupId });
                    table.UniqueConstraint("AK_store_offer_item_id", x => x.id);
                    table.ForeignKey(
                        name: "FK__store_offer_item_groupId__store_offer_group_id",
                        column: x => x.groupId,
                        principalTable: "store_offer_group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_offer_item_data",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    itemId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    type = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    amount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 1u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.itemId });
                    table.ForeignKey(
                        name: "FK__store_offer_item_data_id__store_offer_item_id",
                        column: x => x.id,
                        principalTable: "store_offer_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_offer_item_price",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    currencyId = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    price = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    discountType = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    discountValue = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    field_14 = table.Column<long>(type: "bigint(20)", nullable: false, defaultValue: 0L),
                    expiry = table.Column<long>(type: "bigint(20)", nullable: false, defaultValue: 0L)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.currencyId });
                    table.ForeignKey(
                        name: "FK__store_offer_item_price_id__store_offer_item_id",
                        column: x => x.id,
                        principalTable: "store_offer_item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_creature_loot_chance",
                table: "creature_loot",
                column: "chance");

            migrationBuilder.CreateIndex(
                name: "ix_creature_loot_item",
                table: "creature_loot",
                column: "itemId");

            migrationBuilder.CreateIndex(
                name: "IX_entity_event_eventId",
                table: "entity_event",
                column: "eventId");

            migrationBuilder.CreateIndex(
                name: "IX_entity_event_id",
                table: "entity_event",
                column: "id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_entity_loot_lootGroupId",
                table: "entity_loot",
                column: "lootGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_item_loot_lootGroupId",
                table: "item_loot",
                column: "lootGroupId");

            migrationBuilder.CreateIndex(
                name: "ix_item_salvage_exact_item",
                table: "item_salvage",
                columns: new[] { "purpose", "sourceItemId" });

            migrationBuilder.CreateIndex(
                name: "ix_item_salvage_static",
                table: "item_salvage",
                columns: new[] { "type", "staticId" });

            migrationBuilder.CreateIndex(
                name: "ix_item_salvage_type_level",
                table: "item_salvage",
                columns: new[] { "purpose", "sourceItem2TypeId", "sourceLevel" });

            migrationBuilder.CreateIndex(
                name: "IX_loot_group_parentId",
                table: "loot_group",
                column: "parentId");

            migrationBuilder.CreateIndex(
                name: "parentId",
                table: "store_category",
                column: "parentId");

            migrationBuilder.CreateIndex(
                name: "FK__store_offer_group_category_categoryId__store_category_id",
                table: "store_offer_group_category",
                column: "categoryId");

            migrationBuilder.CreateIndex(
                name: "FK__store_offer_item_groupId__store_offer_group_id",
                table: "store_offer_item",
                column: "groupId");

            migrationBuilder.CreateIndex(
                name: "id",
                table: "store_offer_item",
                column: "id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "creature_info_property");

            migrationBuilder.DropTable(
                name: "creature_info_stat");

            migrationBuilder.DropTable(
                name: "creature_loot");

            migrationBuilder.DropTable(
                name: "disable");

            migrationBuilder.DropTable(
                name: "entity_event");

            migrationBuilder.DropTable(
                name: "entity_loot");

            migrationBuilder.DropTable(
                name: "entity_property");

            migrationBuilder.DropTable(
                name: "entity_script");

            migrationBuilder.DropTable(
                name: "entity_spline");

            migrationBuilder.DropTable(
                name: "entity_stats");

            migrationBuilder.DropTable(
                name: "entity_vendor");

            migrationBuilder.DropTable(
                name: "entity_vendor_category");

            migrationBuilder.DropTable(
                name: "entity_vendor_item");

            migrationBuilder.DropTable(
                name: "item_loot");

            migrationBuilder.DropTable(
                name: "item_salvage");

            migrationBuilder.DropTable(
                name: "loot_item");

            migrationBuilder.DropTable(
                name: "map_entrance");

            migrationBuilder.DropTable(
                name: "store_offer_group_category");

            migrationBuilder.DropTable(
                name: "store_offer_item_data");

            migrationBuilder.DropTable(
                name: "store_offer_item_price");

            migrationBuilder.DropTable(
                name: "tutorial");

            migrationBuilder.DropTable(
                name: "version");

            migrationBuilder.DropTable(
                name: "entity");

            migrationBuilder.DropTable(
                name: "loot_group");

            migrationBuilder.DropTable(
                name: "store_category");

            migrationBuilder.DropTable(
                name: "store_offer_item");

            migrationBuilder.DropTable(
                name: "store_offer_group");
        }
    }
}
