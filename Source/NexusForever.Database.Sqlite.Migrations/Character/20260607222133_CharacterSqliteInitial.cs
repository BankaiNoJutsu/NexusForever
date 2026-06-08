using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NexusForever.Database.Sqlite.Migrations.Character
{
    /// <inheritdoc />
    public partial class CharacterSqliteInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    accountId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    name = table.Column<string>(type: "varchar(50)", nullable: true, defaultValue: ""),
                    sex = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    race = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    @class = table.Column<byte>(name: "class", type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    level = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    factionId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    createTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    lastOnline = table.Column<DateTime>(type: "datetime", nullable: true),
                    locationX = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    locationY = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    locationZ = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    rotationX = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    rotationY = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    rotationZ = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    worldId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    worldZoneId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    title = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    activePath = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    pathActivatedTimestamp = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    activeCostumeIndex = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValue: (sbyte)-1),
                    inputKeySet = table.Column<sbyte>(type: "tinyint(4)", nullable: false, defaultValue: (sbyte)0),
                    activeSpec = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    castingOptions = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    sharedChallengeEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    disableOtherPlayersCombatLogs = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    combatLogDisableFlags = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    pvpFlagDisableUntilUtc = table.Column<DateTime>(type: "datetime", nullable: true),
                    innateIndex = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    timePlayedTotal = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    timePlayedLevel = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    deleteTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    originalName = table.Column<string>(type: "varchar(50)", nullable: true),
                    totalXp = table.Column<uint>(type: "int", nullable: false, defaultValue: 0u),
                    restBonusXp = table.Column<uint>(type: "int", nullable: false, defaultValue: 0u),
                    guildAffiliation = table.Column<ulong>(type: "INTEGER", nullable: true),
                    flags = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    isOnline = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_create",
                columns: table => new
                {
                    race = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    creationStart = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    faction = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    worldId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    x = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    y = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    z = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    rx = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    ry = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    rz = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    comment = table.Column<string>(type: "varchar(200)", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.race, x.faction, x.creationStart });
                });

            migrationBuilder.CreateTable(
                name: "chat_channel",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    name = table.Column<string>(type: "varchar(20)", nullable: true, defaultValue: ""),
                    password = table.Column<string>(type: "varchar(20)", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guild",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    flags = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    name = table.Column<string>(type: "varchar(30)", nullable: true, defaultValue: ""),
                    leaderId = table.Column<ulong>(type: "bigint(20)", nullable: true),
                    createTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    deleteTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    originalName = table.Column<string>(type: "varchar(30)", nullable: true),
                    originalLeaderId = table.Column<ulong>(type: "bigint(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leaderboard_pve_score",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    characterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    realmId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    matchingGameMapId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    primeLevel = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    completionTime = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    rewardedTier = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    playerName = table.Column<string>(type: "varchar(64)", nullable: true, defaultValue: ""),
                    playerClass = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    guildId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    teamMembersJson = table.Column<string>(type: "varchar(512)", nullable: true),
                    recordedUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaderboard_pve_score", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "leaderboard_pvp_score",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    characterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    realmId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    rating = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    playerName = table.Column<string>(type: "varchar(64)", nullable: true, defaultValue: ""),
                    playerClass = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    guildId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    teamMembersJson = table.Column<string>(type: "varchar(512)", nullable: true),
                    recordedUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leaderboard_pvp_score", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "marketplace_commodity_order",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    ownerCharacterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    item2Id = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    quantity = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    pricePerUnit = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    price = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    isBuyOrder = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    forceImmediate = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    listTime = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    expirationTime = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "property_base",
                columns: table => new
                {
                    type = table.Column<uint>(type: "INTEGER", nullable: false, defaultValueSql: "'0'"),
                    subtype = table.Column<uint>(type: "INTEGER", nullable: false, defaultValueSql: "'0'"),
                    property = table.Column<uint>(type: "INTEGER", nullable: false, defaultValueSql: "'0'"),
                    modType = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValueSql: "'0'"),
                    value = table.Column<float>(type: "REAL", nullable: false, defaultValueSql: "'0'"),
                    note = table.Column<string>(type: "varchar(100)", nullable: false, defaultValueSql: "''")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.type, x.subtype, x.property });
                });

            migrationBuilder.CreateTable(
                name: "realm_bank_item",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    accountId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    realmId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    itemId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    bagIndex = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    stackCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    charges = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    durability = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    expirationTimeLeft = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    soulbound = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_realm_bank_item", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "character_achievement",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    achievementId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    data0 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    data1 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    dateCompleted = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.achievementId });
                    table.ForeignKey(
                        name: "FK__character_achievement_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_action_set_amp",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    specIndex = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    ampId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.specIndex, x.ampId });
                    table.ForeignKey(
                        name: "FK__character_action_set_amp_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_action_set_shortcut",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    specIndex = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    location = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    shortcutType = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    objectId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    tier = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.specIndex, x.location });
                    table.ForeignKey(
                        name: "FK__character_action_set_shortcut_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_appearance",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    slot = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    displayId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.slot });
                    table.ForeignKey(
                        name: "FK__character_appearance_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_bone",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    boneIndex = table.Column<byte>(type: "tinyint(4)", nullable: false),
                    bone = table.Column<float>(type: "float", nullable: false, defaultValue: 0f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.boneIndex });
                    table.ForeignKey(
                        name: "FK_character_bone_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_challenge",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    challengeId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    activated = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    onCooldown = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    leftArea = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    currentCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    currentTier = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    lastRewardTier = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    completionCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    activeTimerSeconds = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    cooldownTimerSeconds = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0),
                    areaFailTimerSeconds = table.Column<double>(type: "double", nullable: false, defaultValue: 0.0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.challengeId });
                    table.ForeignKey(
                        name: "FK__character_challenge_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_costume",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    index = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    visibilityMask = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    timestamp = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.index });
                    table.ForeignKey(
                        name: "FK__character_costume_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_currency",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    currencyId = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    amount = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.currencyId });
                    table.ForeignKey(
                        name: "FK_character_currency_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_customisation",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    label = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    value = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.label });
                    table.ForeignKey(
                        name: "FK__character_customisation_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_datacube",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    datacube = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    progress = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.type, x.datacube });
                    table.ForeignKey(
                        name: "FK__character_datacube_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_entitlement",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    entitlementId = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    amount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.entitlementId });
                    table.ForeignKey(
                        name: "FK__character_entitlement_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_galactic_archive",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    archiveArticleId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    unlockedFlags = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    viewedFlags = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.archiveArticleId });
                    table.ForeignKey(
                        name: "FK__character_galactic_archive_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_keybinding",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    inputActionId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    deviceEnum00 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    deviceEnum01 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    deviceEnum02 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    code00 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    code01 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    code02 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    metaKeys00 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    metaKeys01 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    metaKeys02 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    eventTypeEnum00 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    eventTypeEnum01 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    eventTypeEnum02 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.inputActionId });
                    table.ForeignKey(
                        name: "FK__character_keybinding_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mail",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    recipientId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    senderType = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    contentType = table.Column<byte>(type: "tinyint(8)", nullable: false, defaultValue: (byte)0),
                    senderId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    subject = table.Column<string>(type: "varchar(200)", nullable: false, defaultValue: ""),
                    message = table.Column<string>(type: "varchar(2000)", nullable: false, defaultValue: ""),
                    textEntrySubject = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    textEntryMessage = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    creatureId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    currencyType = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    currencyAmount = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    isCashOnDelivery = table.Column<byte>(type: "tinyint(8)", nullable: false, defaultValue: (byte)0),
                    hasPaidOrCollectedCurrency = table.Column<byte>(type: "tinyint(8)", nullable: false, defaultValue: (byte)0),
                    flags = table.Column<byte>(type: "tinyint(8)", nullable: false, defaultValue: (byte)0),
                    deliveryTime = table.Column<byte>(type: "tinyint(8)", nullable: false, defaultValue: (byte)0),
                    createTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_mail", x => x.id);
                    table.ForeignKey(
                        name: "FK__character_mail_recipientId__character_id",
                        column: x => x.recipientId,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_matching_penalty",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    isPvp = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    matchType = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    spell4Id = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    expiresAtUtc = table.Column<DateTime>(type: "datetime", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__character_matching_penalty_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_path",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    path = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    unlocked = table.Column<byte>(type: "tinyint(1)", nullable: false, defaultValue: (byte)0),
                    totalXp = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    levelRewarded = table.Column<byte>(type: "tinyint(4)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.path });
                    table.ForeignKey(
                        name: "FK__character_path_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_path_mission",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    pathMissionId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    pathEpisodeId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    state = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    completed = table.Column<byte>(type: "tinyint(1)", nullable: false, defaultValue: (byte)0),
                    progressCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    progressData = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    xp = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.pathMissionId });
                    table.ForeignKey(
                        name: "FK__character_path_mission_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_pet_customisation",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    type = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    objectId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    name = table.Column<string>(type: "varchar(128)", nullable: false, defaultValue: ""),
                    flairIdMask = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.type, x.objectId });
                    table.ForeignKey(
                        name: "FK__character_pet_customisation_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_pet_flair",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    petFlairId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.petFlairId });
                    table.ForeignKey(
                        name: "FK__character_pet_flair_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_quest",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    questId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    state = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    flags = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    timer = table.Column<uint>(type: "int(10)", nullable: true),
                    reset = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.questId });
                    table.ForeignKey(
                        name: "FK__character_quest_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_reputation",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    factionId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    amount = table.Column<float>(type: "float", nullable: false, defaultValue: 0f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.factionId });
                    table.ForeignKey(
                        name: "FK__character_reputation_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_schematic",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    tradeskillSchematic2Id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    discovered = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    discoveryCoordinateX = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    discoveryCoordinateY = table.Column<float>(type: "float", nullable: false, defaultValue: 0f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.tradeskillSchematic2Id });
                    table.ForeignKey(
                        name: "FK__character_schematic_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_spell",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    spell4BaseId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    tier = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.spell4BaseId });
                    table.ForeignKey(
                        name: "FK__character_spell_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_stats",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    stat = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    value = table.Column<float>(type: "float", nullable: false, defaultValue: 0f)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.stat });
                    table.ForeignKey(
                        name: "FK__character_stats_stat_id_character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_title",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    title = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    timeRemaining = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    revoked = table.Column<byte>(type: "tinyint(4)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.title });
                    table.ForeignKey(
                        name: "FK__character_title_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_tradeskill",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    tradeskillId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    tradeskillXp = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    isActive = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    propertyProficiencyFlags = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentPoints = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier00 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier01 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier02 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier03 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier04 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier05 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier06 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier07 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier08 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    talentTier09 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.tradeskillId });
                    table.ForeignKey(
                        name: "FK__character_tradeskill_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_tradeskill_materials",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    materialId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    amount = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.materialId });
                    table.ForeignKey(
                        name: "FK__character_tradeskill_material_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_zonemap_hexgroup",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    zoneMap = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    hexGroup = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.zoneMap, x.hexGroup });
                    table.ForeignKey(
                        name: "FK__character_zonemap_hexgroup_id__character_id",
                        column: x => x.id,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "item",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    ownerId = table.Column<ulong>(type: "bigint(20)", nullable: true),
                    itemId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    location = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    bagIndex = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    stackCount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    charges = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    durability = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    expirationTimeLeft = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    soulbound = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    microchipIds = table.Column<string>(type: "varchar(128)", nullable: true, defaultValue: ""),
                    runeSlots = table.Column<string>(type: "varchar(256)", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_item", x => x.id);
                    table.ForeignKey(
                        name: "FK__item_ownerId__character_id",
                        column: x => x.ownerId,
                        principalTable: "character",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_channel_member",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    flags = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.characterId });
                    table.ForeignKey(
                        name: "FK__chat_channel_member_id__chat_channel_id",
                        column: x => x.id,
                        principalTable: "chat_channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guild_achievement",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    achievementId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    data0 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    data1 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    dateCompleted = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.achievementId });
                    table.ForeignKey(
                        name: "FK__guild_achievement_id__guild_id",
                        column: x => x.id,
                        principalTable: "guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guild_guild_data",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    motd = table.Column<string>(type: "varchar(200)", nullable: true, defaultValue: ""),
                    additionalInfo = table.Column<string>(type: "varchar(400)", nullable: true, defaultValue: ""),
                    backgroundIconPartId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    foregroundIconPartId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    scanLinesPartId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__guild_guild_data_id__guild_id",
                        column: x => x.id,
                        principalTable: "guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guild_member",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    rank = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    note = table.Column<string>(type: "varchar(32)", nullable: true, defaultValue: ""),
                    communityPlotReservation = table.Column<int>(type: "int(11)", nullable: false, defaultValue: -1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.characterId });
                    table.ForeignKey(
                        name: "FK__guild_member_id__guild_id",
                        column: x => x.id,
                        principalTable: "guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "guild_rank",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    index = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    name = table.Column<string>(type: "varchar(16)", nullable: true, defaultValue: ""),
                    permission = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    bankWithdrawPermission = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    moneyWithdrawalLimit = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    repairLimit = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.index });
                    table.ForeignKey(
                        name: "FK__guild_rank_id__guild_id",
                        column: x => x.id,
                        principalTable: "guild",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "residence",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    ownerId = table.Column<ulong>(type: "bigint(20)", nullable: true),
                    guildOwnerId = table.Column<ulong>(type: "bigint(20)", nullable: true),
                    propertyInfoId = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    name = table.Column<string>(type: "varchar(50)", nullable: false, defaultValue: ""),
                    privacyLevel = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    wallpaperId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    roofDecorInfoId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    entrywayDecorInfoId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    doorDecorInfoId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    groundWallpaperId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    musicId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    skyWallpaperId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    flags = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    resourceSharing = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    gardenSharing = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_residence", x => x.id);
                    table.ForeignKey(
                        name: "FK__residence_guildOwnerId__guild_id",
                        column: x => x.guildOwnerId,
                        principalTable: "guild",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__residence_ownerId__character_id",
                        column: x => x.ownerId,
                        principalTable: "character",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "character_costume_item",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    index = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    slot = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    item2Id = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    dyeData = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.index, x.slot });
                    table.ForeignKey(
                        name: "FK__character_costume_item_id-index__character_costume_id-index",
                        columns: x => new { x.id, x.index },
                        principalTable: "character_costume",
                        principalColumns: new[] { "id", "index" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_quest_objective",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    questId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
                    index = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    progress = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    timer = table.Column<uint>(type: "int(10)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.questId, x.index });
                    table.ForeignKey(
                        name: "FK__character_quest_objective_id__character_id",
                        columns: x => new { x.id, x.questId },
                        principalTable: "character_quest",
                        principalColumns: new[] { "id", "questId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_mail_attachment",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    index = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    itemGuid = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.index });
                    table.ForeignKey(
                        name: "FK__character_mail_attachment_id__character_mail_id",
                        column: x => x.id,
                        principalTable: "character_mail",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__character_mail_attachment_itemGuid__item_id",
                        column: x => x.itemGuid,
                        principalTable: "item",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "marketplace_auction",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul)
                        .Annotation("Sqlite:Autoincrement", true),
                    ownerCharacterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    itemId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    minimumBid = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    buyoutPrice = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    currentBid = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    topBidderCharacterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    expirationTime = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    item2Id = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    quantity = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    worldRequirementItem2Id = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    circuitData = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    glyphData = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    thresholdData = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    unknown2 = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    unknownArray = table.Column<string>(type: "varchar(255)", nullable: true, defaultValue: "")
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
                });

            migrationBuilder.CreateTable(
                name: "residence_decor",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    decorId = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    decorInfoId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    decorType = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    decorData = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    hookBagIndex = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    hookIndex = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    plotIndex = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 2147483647u),
                    scale = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    x = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    y = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    z = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    qx = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    qy = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    qz = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    qw = table.Column<float>(type: "float", nullable: false, defaultValue: 0f),
                    activePropUnitId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    decorParentId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    colourShiftId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.decorId });
                    table.ForeignKey(
                        name: "FK__residence_decor_id__residence_id",
                        column: x => x.id,
                        principalTable: "residence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "residence_neighbor",
                columns: table => new
                {
                    residenceId = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    neighborCharacterId = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    permissionLevel = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.residenceId, x.neighborCharacterId });
                    table.ForeignKey(
                        name: "FK__residence_neighbor_residenceId__residence_id",
                        column: x => x.residenceId,
                        principalTable: "residence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "residence_plot",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    index = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    plotInfoId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    plugItemId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    plugFacing = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    buildState = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.index });
                    table.ForeignKey(
                        name: "FK__residence_plot_id__residence_id",
                        column: x => x.id,
                        principalTable: "residence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "character_create",
                columns: new[] { "creationStart", "faction", "race", "comment", "rx", "worldId", "x", "y", "z" },
                values: new object[,]
                {
                    { (byte)3, (ushort)166, (byte)1, "Dominion Cassian - Veteran", -0.45682f, 1387u, -3835.341f, -980.2174f, -6050.524f },
                    { (byte)4, (ushort)166, (byte)1, "Dominion Cassian - Novice", -2.751458f, 3460u, 29.1286f, -853.8716f, -560.188f },
                    { (byte)5, (ushort)166, (byte)1, "Dominion Cassian - Level 50", -0.7632219f, 22u, -3343.58f, -887.4646f, -536.03f },
                    { (byte)3, (ushort)167, (byte)1, "Exile Human - Veteran", 0.317613f, 426u, 4110.71f, -658.6249f, -5145.48f },
                    { (byte)4, (ushort)167, (byte)1, "Exile Human - Novice", -2.751458f, 3460u, 29.1286f, -853.8716f, -560.188f }
                });

            migrationBuilder.InsertData(
                table: "character_create",
                columns: new[] { "creationStart", "faction", "race", "comment", "worldId", "x", "y", "z" },
                values: new object[] { (byte)5, (ushort)167, (byte)1, "Exile Human - Level 50", 51u, 4074.34f, -797.8368f, -2399.37f });

            migrationBuilder.InsertData(
                table: "character_create",
                columns: new[] { "creationStart", "faction", "race", "comment", "rx", "worldId", "x", "y", "z" },
                values: new object[,]
                {
                    { (byte)3, (ushort)167, (byte)3, "Exile Granok- Veteran", 0.317613f, 426u, 4110.71f, -658.6249f, -5145.48f },
                    { (byte)4, (ushort)167, (byte)3, "Exile Granok - Novice", -2.751458f, 3460u, 29.1286f, -853.8716f, -560.188f }
                });

            migrationBuilder.InsertData(
                table: "character_create",
                columns: new[] { "creationStart", "faction", "race", "comment", "worldId", "x", "y", "z" },
                values: new object[] { (byte)5, (ushort)167, (byte)3, "Exile Granok - Level 50", 51u, 4074.34f, -797.8368f, -2399.37f });

            migrationBuilder.InsertData(
                table: "character_create",
                columns: new[] { "creationStart", "faction", "race", "comment", "rx", "worldId", "x", "y", "z" },
                values: new object[,]
                {
                    { (byte)3, (ushort)167, (byte)4, "Exile Aurin - Veteran", -1.1214035f, 990u, -771.823f, -904.2852f, -2269.56f },
                    { (byte)4, (ushort)167, (byte)4, "Exile Aurin - Novice", -2.751458f, 3460u, 29.1286f, -853.8716f, -560.188f }
                });

            migrationBuilder.InsertData(
                table: "character_create",
                columns: new[] { "creationStart", "faction", "race", "comment", "worldId", "x", "y", "z" },
                values: new object[] { (byte)5, (ushort)167, (byte)4, "Exile Aurin - Level 50", 51u, 4074.34f, -797.8368f, -2399.37f });

            migrationBuilder.InsertData(
                table: "character_create",
                columns: new[] { "creationStart", "faction", "race", "comment", "rx", "worldId", "x", "y", "z" },
                values: new object[,]
                {
                    { (byte)3, (ushort)166, (byte)5, "Dominion Draken - Veteran", -2.215535f, 870u, -8261.398f, -995.471f, -242.3648f },
                    { (byte)4, (ushort)166, (byte)5, "Dominion Draken - Novice", -2.751458f, 3460u, 29.1286f, -853.8716f, -560.188f },
                    { (byte)5, (ushort)166, (byte)5, "Dominion Draken - Level 50", -0.7632219f, 22u, -3343.58f, -887.4646f, -536.03f },
                    { (byte)3, (ushort)166, (byte)12, "Dominion Mechari - Veteran", -0.45682f, 1387u, -3835.341f, -980.2174f, -6050.524f },
                    { (byte)4, (ushort)166, (byte)12, "Dominion Mechari - Novice", -2.751458f, 3460u, 29.1286f, -853.8716f, -560.188f },
                    { (byte)5, (ushort)166, (byte)12, "Dominion Mechari - Level 50", -0.7632219f, 22u, -3343.58f, -887.4646f, -536.03f },
                    { (byte)3, (ushort)166, (byte)13, "Dominion Chua - Veteran", -2.215535f, 870u, -8261.398f, -995.471f, -242.3648f },
                    { (byte)4, (ushort)166, (byte)13, "Dominion Chua - Novice", -2.751458f, 3460u, 29.1286f, -853.8716f, -560.188f },
                    { (byte)5, (ushort)166, (byte)13, "Dominion Chua - Level 50", -0.7632219f, 22u, -3343.58f, -887.4646f, -536.03f },
                    { (byte)3, (ushort)167, (byte)16, "Exile Mordesh - Veteran", -1.1214035f, 990u, -771.823f, -904.2852f, -2269.56f },
                    { (byte)4, (ushort)167, (byte)16, "Exile Mordesh - Novice", -2.751458f, 3460u, 29.1286f, -853.8716f, -560.188f }
                });

            migrationBuilder.InsertData(
                table: "character_create",
                columns: new[] { "creationStart", "faction", "race", "comment", "worldId", "x", "y", "z" },
                values: new object[] { (byte)5, (ushort)167, (byte)16, "Exile Mordesh - Level 50", 51u, 4074.34f, -797.8368f, -2399.37f });

            migrationBuilder.InsertData(
                table: "property_base",
                columns: new[] { "property", "subtype", "type", "note" },
                values: new object[,]
                {
                    { 0u, 0u, 0u, "Player - Base Strength" },
                    { 1u, 0u, 0u, "Player - Base Dexterity" },
                    { 2u, 0u, 0u, "Player - Base Technology" },
                    { 3u, 0u, 0u, "Player - Base Magic" },
                    { 4u, 0u, 0u, "Player - Base Wisdom" }
                });

            migrationBuilder.InsertData(
                table: "property_base",
                columns: new[] { "property", "subtype", "type", "modType", "note", "value" },
                values: new object[] { 7u, 0u, 0u, (ushort)3, "Player - Base HP per Level", 200f });

            migrationBuilder.InsertData(
                table: "property_base",
                columns: new[] { "property", "subtype", "type", "note", "value" },
                values: new object[,]
                {
                    { 9u, 0u, 0u, "Player - Base Endurance", 500f },
                    { 16u, 0u, 0u, "Player - Base Endurance Regen", 0.0225f },
                    { 17u, 0u, 0u, "Warrior - Base Kinetic Energy Regen", 1f }
                });

            migrationBuilder.InsertData(
                table: "property_base",
                columns: new[] { "property", "subtype", "type", "modType", "note", "value" },
                values: new object[,]
                {
                    { 35u, 0u, 0u, (ushort)3, "Player - Base Assault Rating per Level", 18f },
                    { 36u, 0u, 0u, (ushort)3, "Player - Base Support Rating per Level", 18f }
                });

            migrationBuilder.InsertData(
                table: "property_base",
                columns: new[] { "property", "subtype", "type", "note", "value" },
                values: new object[,]
                {
                    { 38u, 0u, 0u, "Player - Base Dash Energy", 200f },
                    { 39u, 0u, 0u, "Player - Base Dash Energy Regen", 0.045f }
                });

            migrationBuilder.InsertData(
                table: "property_base",
                columns: new[] { "property", "subtype", "type", "note" },
                values: new object[] { 41u, 0u, 0u, "Player - Shield Capacity Base" });

            migrationBuilder.InsertData(
                table: "property_base",
                columns: new[] { "property", "subtype", "type", "note", "value" },
                values: new object[,]
                {
                    { 100u, 0u, 0u, "Player - Base Movement Speed", 1f },
                    { 101u, 0u, 0u, "Player - Base Avoid Chance", 0.05f },
                    { 102u, 0u, 0u, "Player - Base Crit Chance", 0.05f }
                });

            migrationBuilder.InsertData(
                table: "property_base",
                columns: new[] { "property", "subtype", "type", "note" },
                values: new object[,]
                {
                    { 107u, 0u, 0u, "Player - Base Focus Recovery In Combat" },
                    { 108u, 0u, 0u, "Player - Base Focus Recovery Out of Combat" }
                });

            migrationBuilder.InsertData(
                table: "property_base",
                columns: new[] { "property", "subtype", "type", "note", "value" },
                values: new object[,]
                {
                    { 112u, 0u, 0u, "Player - Base Multi-Hit Amount", 0.3f },
                    { 130u, 0u, 0u, "Player - Base Gravity Multiplier", 0.8f },
                    { 150u, 0u, 0u, "Player - Base Damage Taken Offset - Physical", 1f },
                    { 151u, 0u, 0u, "Player - Base Damage Taken Offset - Tech", 1f },
                    { 152u, 0u, 0u, "Player - Base Damage Taken Offset - Magic", 1f },
                    { 154u, 0u, 0u, "Player - Base Mutli-Hit Chance", 0.05f },
                    { 155u, 0u, 0u, "Player - Base Damage Reflect Amount", 0.05f },
                    { 191u, 0u, 0u, "Player - Base Mount Movement Speed", 1f },
                    { 195u, 0u, 0u, "Player - Base Glance Amount", 0.3f },
                    { 10u, 1u, 1u, "Class - Warrior - Base Kinetic Energy Cap", 1000f },
                    { 10u, 2u, 1u, "Class - Engineer - Base Volatile Energy Cap", 100f },
                    { 10u, 3u, 1u, "Class - Esper - Base Psi Point Cap", 5f },
                    { 10u, 4u, 1u, "Class - Medic - Base Medic Core Cap", 4f },
                    { 12u, 5u, 1u, "Class - Stalker - Base Suit Power Cap", 100f },
                    { 19u, 5u, 1u, "Class - Stalker - Base Suit Power Regeneration Rate", 0.035f },
                    { 13u, 7u, 1u, "Class - Spellslinger - Base Spell Power Cap", 100f }
                });

            migrationBuilder.CreateIndex(
                name: "accountId",
                table: "character",
                column: "accountId");

            migrationBuilder.CreateIndex(
                name: "FK__character_mail_recipientId__character_id",
                table: "character_mail",
                column: "recipientId");

            migrationBuilder.CreateIndex(
                name: "itemGuid",
                table: "character_mail_attachment",
                column: "itemGuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "FK__item_ownerId__character_id",
                table: "item",
                column: "ownerId");

            migrationBuilder.CreateIndex(
                name: "IX_marketplace_auction_itemId",
                table: "marketplace_auction",
                column: "itemId");

            migrationBuilder.CreateIndex(
                name: "IX_residence_guildOwnerId",
                table: "residence",
                column: "guildOwnerId");

            migrationBuilder.CreateIndex(
                name: "ownerId",
                table: "residence",
                column: "ownerId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_achievement");

            migrationBuilder.DropTable(
                name: "character_action_set_amp");

            migrationBuilder.DropTable(
                name: "character_action_set_shortcut");

            migrationBuilder.DropTable(
                name: "character_appearance");

            migrationBuilder.DropTable(
                name: "character_bone");

            migrationBuilder.DropTable(
                name: "character_challenge");

            migrationBuilder.DropTable(
                name: "character_costume_item");

            migrationBuilder.DropTable(
                name: "character_create");

            migrationBuilder.DropTable(
                name: "character_currency");

            migrationBuilder.DropTable(
                name: "character_customisation");

            migrationBuilder.DropTable(
                name: "character_datacube");

            migrationBuilder.DropTable(
                name: "character_entitlement");

            migrationBuilder.DropTable(
                name: "character_galactic_archive");

            migrationBuilder.DropTable(
                name: "character_keybinding");

            migrationBuilder.DropTable(
                name: "character_mail_attachment");

            migrationBuilder.DropTable(
                name: "character_matching_penalty");

            migrationBuilder.DropTable(
                name: "character_path");

            migrationBuilder.DropTable(
                name: "character_path_mission");

            migrationBuilder.DropTable(
                name: "character_pet_customisation");

            migrationBuilder.DropTable(
                name: "character_pet_flair");

            migrationBuilder.DropTable(
                name: "character_quest_objective");

            migrationBuilder.DropTable(
                name: "character_reputation");

            migrationBuilder.DropTable(
                name: "character_schematic");

            migrationBuilder.DropTable(
                name: "character_spell");

            migrationBuilder.DropTable(
                name: "character_stats");

            migrationBuilder.DropTable(
                name: "character_title");

            migrationBuilder.DropTable(
                name: "character_tradeskill");

            migrationBuilder.DropTable(
                name: "character_tradeskill_materials");

            migrationBuilder.DropTable(
                name: "character_zonemap_hexgroup");

            migrationBuilder.DropTable(
                name: "chat_channel_member");

            migrationBuilder.DropTable(
                name: "guild_achievement");

            migrationBuilder.DropTable(
                name: "guild_guild_data");

            migrationBuilder.DropTable(
                name: "guild_member");

            migrationBuilder.DropTable(
                name: "guild_rank");

            migrationBuilder.DropTable(
                name: "leaderboard_pve_score");

            migrationBuilder.DropTable(
                name: "leaderboard_pvp_score");

            migrationBuilder.DropTable(
                name: "marketplace_auction");

            migrationBuilder.DropTable(
                name: "marketplace_commodity_order");

            migrationBuilder.DropTable(
                name: "property_base");

            migrationBuilder.DropTable(
                name: "realm_bank_item");

            migrationBuilder.DropTable(
                name: "residence_decor");

            migrationBuilder.DropTable(
                name: "residence_neighbor");

            migrationBuilder.DropTable(
                name: "residence_plot");

            migrationBuilder.DropTable(
                name: "character_costume");

            migrationBuilder.DropTable(
                name: "character_mail");

            migrationBuilder.DropTable(
                name: "character_quest");

            migrationBuilder.DropTable(
                name: "chat_channel");

            migrationBuilder.DropTable(
                name: "item");

            migrationBuilder.DropTable(
                name: "residence");

            migrationBuilder.DropTable(
                name: "guild");

            migrationBuilder.DropTable(
                name: "character");
        }
    }
}
