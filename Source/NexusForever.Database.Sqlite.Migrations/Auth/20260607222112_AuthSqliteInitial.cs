using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NexusForever.Database.Sqlite.Migrations.Auth
{
    /// <inheritdoc />
    public partial class AuthSqliteInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "account",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    email = table.Column<string>(type: "varchar(128)", nullable: false, defaultValue: ""),
                    s = table.Column<string>(type: "varchar(32)", nullable: false, defaultValue: ""),
                    v = table.Column<string>(type: "varchar(512)", nullable: false, defaultValue: ""),
                    gameToken = table.Column<string>(type: "varchar(32)", nullable: false, defaultValue: ""),
                    sessionKey = table.Column<string>(type: "varchar(32)", nullable: false, defaultValue: ""),
                    createTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_credd_history",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    accountId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    createdUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    operation = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    isInitiator = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    identityRealmId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    identityCharacterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    counterpartyRealmId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    counterpartyCharacterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    creditAmount = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "account_credd_order",
                columns: table => new
                {
                    orderId = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    accountId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    characterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    realmId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    creditAmount = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    isBuyOrder = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.orderId);
                });

            migrationBuilder.CreateTable(
                name: "permission",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "varchar(64)", nullable: true, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "role",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "varchar(64)", nullable: true, defaultValue: ""),
                    flags = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "server",
                columns: table => new
                {
                    id = table.Column<byte>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "varchar(64)", nullable: false, defaultValue: "NexusForever"),
                    host = table.Column<string>(type: "varchar(64)", nullable: false, defaultValue: "127.0.0.1"),
                    port = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)24000),
                    type = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_server", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "server_message",
                columns: table => new
                {
                    index = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    language = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    message = table.Column<string>(type: "varchar(256)", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.index, x.language });
                });

            migrationBuilder.CreateTable(
                name: "account_costume_unlock",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    itemId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    timestamp = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.itemId });
                    table.ForeignKey(
                        name: "FK__account_costume_item_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_currency",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    currencyId = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    amount = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.currencyId });
                    table.ForeignKey(
                        name: "FK__account_currency_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_daily_login",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10)", nullable: false),
                    loginDaysTotal = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    rewardsAvailable = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    lastClaimedLoginDay = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    lastRewardItemKey = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    premiumKeyStatus = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    secondsUntilNextKey = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    lastClaimUtc = table.Column<DateTime>(type: "datetime", nullable: true),
                    lastDayIncrementUtc = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__account_daily_login_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_entitlement",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    entitlementId = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    amount = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.entitlementId });
                    table.ForeignKey(
                        name: "FK__account_entitlement_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_external_reference",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10)", nullable: false),
                    type = table.Column<string>(type: "varchar(64)", nullable: false),
                    value = table.Column<string>(type: "varchar(512)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.type });
                    table.ForeignKey(
                        name: "FK__account_external_reference_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_fortune_session",
                columns: table => new
                {
                    id = table.Column<uint>(type: "int(10)", nullable: false),
                    card0AccountItemId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    card1AccountItemId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    card2AccountItemId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    card0Rarity = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    card1Rarity = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    card2Rarity = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    card0Flipped = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card1Flipped = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card2Flipped = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card0Granted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card1Granted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    card2Granted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__account_fortune_session_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_generic_unlock",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    entry = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    timestamp = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.entry });
                    table.ForeignKey(
                        name: "FK__account_generic_unlock_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_inventory",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    inventoryId = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    accountItemId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    claimState = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    unknown1 = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    targetRealmId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    targetCharacterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    createTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.inventoryId });
                    table.ForeignKey(
                        name: "FK__account_inventory_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_item_cooldown",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    cooldownGroupId = table.Column<uint>(type: "int(10)", nullable: false),
                    timestamp = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "CURRENT_TIMESTAMP"),
                    duration = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.cooldownGroupId });
                    table.ForeignKey(
                        name: "FK__account_item_cooldown_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_keybinding",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    inputActionId = table.Column<ushort>(type: "INTEGER", nullable: false, defaultValue: (ushort)0),
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
                        name: "FK__account_keybinding_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_pending_item",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    pendingItemId = table.Column<ulong>(type: "INTEGER", nullable: false, defaultValue: 0ul),
                    groupName = table.Column<string>(type: "varchar(128)", nullable: false, defaultValue: ""),
                    accountItemId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    senderAccountId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    senderRealmId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    senderCharacterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    targetRealmId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    targetCharacterId = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    claimState = table.Column<byte>(type: "tinyint(3)", nullable: false, defaultValue: (byte)0),
                    unknown1 = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    createTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.pendingItemId });
                    table.ForeignKey(
                        name: "FK__account_pending_item_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_reward_rotation_grant",
                columns: table => new
                {
                    accountId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    rewardRotationIndex = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    contentId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    rewardKeyId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    rewardType = table.Column<byte>(type: "INTEGER", nullable: false, defaultValue: (byte)0),
                    grantFlags = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    grantedUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.accountId, x.rewardRotationIndex, x.contentId, x.rewardKeyId, x.rewardType });
                    table.ForeignKey(
                        name: "FK__account_reward_rotation_grant_accountId__account_id",
                        column: x => x.accountId,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_store_purchase_history",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "bigint(20)", nullable: false),
                    accountId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    offerId = table.Column<uint>(type: "int(10)", nullable: false, defaultValue: 0u),
                    currencyId = table.Column<ushort>(type: "smallint(5)", nullable: false, defaultValue: (ushort)0),
                    price = table.Column<ulong>(type: "bigint(20)", nullable: false, defaultValue: 0ul),
                    purchasedUtc = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                    table.ForeignKey(
                        name: "FK__account_store_purchase_history_accountId__account_id",
                        column: x => x.accountId,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_suspension",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    banId = table.Column<uint>(type: "INTEGER", nullable: false),
                    startTime = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    endTime = table.Column<DateTime>(type: "datetime", nullable: true),
                    Reason = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.id, x.banId });
                    table.ForeignKey(
                        name: "FK__account_suspension_account_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_permission",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    permissionId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_permission", x => new { x.id, x.permissionId });
                    table.ForeignKey(
                        name: "FK__account_permission_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__account_permission_permission_id__permission_id",
                        column: x => x.permissionId,
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "account_role",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    roleId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_role", x => new { x.id, x.roleId });
                    table.ForeignKey(
                        name: "FK__account_role_id__account_id",
                        column: x => x.id,
                        principalTable: "account",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__account_role_role_id__role_id",
                        column: x => x.roleId,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permission",
                columns: table => new
                {
                    id = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u),
                    permissionId = table.Column<uint>(type: "INTEGER", nullable: false, defaultValue: 0u)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permission", x => new { x.id, x.permissionId });
                    table.ForeignKey(
                        name: "FK__role_permission_id__role_id",
                        column: x => x.id,
                        principalTable: "role",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__role_permission_permission_id__permission_id",
                        column: x => x.permissionId,
                        principalTable: "permission",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "permission",
                columns: new[] { "id", "name" },
                values: new object[,]
                {
                    { 1u, "Category: Account" },
                    { 2u, "Command: AccountCreate" },
                    { 3u, "Command: AccountDelete" },
                    { 4u, "Category: Help" },
                    { 5u, "Command: CharacterSave" },
                    { 6u, "Command: ItemAdd" },
                    { 7u, "Category: RBAC" },
                    { 8u, "Category: RBACAccount" },
                    { 9u, "Category: RBACAccountPermission" },
                    { 10u, "Command: RBACAccountPermissionGrant" },
                    { 11u, "Command: RBACAccountPermissionRevoke" },
                    { 12u, "Category: RBACAccountRole" },
                    { 13u, "Command: RBACAccountRoleGrant" },
                    { 14u, "Command: RBACAccountRoleRevoke" },
                    { 15u, "Category: Achievement" },
                    { 16u, "Command: AchievementGrant" },
                    { 17u, "Command: AchievementUpdate" },
                    { 18u, "Category: Broadcast" },
                    { 19u, "Command: BroadcastMessage" },
                    { 20u, "Category: Character" },
                    { 21u, "Command: CharacterXP" },
                    { 22u, "Command: CharacterLevel" },
                    { 23u, "Category: Currency" },
                    { 24u, "Category: CurrencyAccount" },
                    { 25u, "Command: CurrencyAccountAdd" },
                    { 26u, "Command: CurrencyAccountList" },
                    { 27u, "Category: CurrencyCharacter" },
                    { 28u, "Command: CurrencyCharacterAdd" },
                    { 29u, "Command: CurrencyCharacterList" },
                    { 30u, "Category: Disable" },
                    { 31u, "Command: DisableInfo" },
                    { 32u, "Command: DisableReload" },
                    { 33u, "Category: Door" },
                    { 34u, "Command: DoorOpen" },
                    { 35u, "Command: DoorClose" },
                    { 36u, "Category: Entitlement" },
                    { 37u, "Category: EntitlementCharacter" },
                    { 39u, "Command: EntitlementCharacterList" },
                    { 40u, "Category: EntitlementAccount" },
                    { 41u, "Command: EntitlementAdd" },
                    { 42u, "Command: EntitlementAccountList" },
                    { 43u, "Category: Entity" },
                    { 44u, "Command: EntityInfo" },
                    { 45u, "Command: EntityProperties" },
                    { 46u, "Category: Generic" },
                    { 47u, "Command: GenericUnlock" },
                    { 48u, "Command: GenericUnlockAll" },
                    { 49u, "Command: GenericList" },
                    { 50u, "Category: Teleport" },
                    { 51u, "Command: TeleportCoordinates" },
                    { 52u, "Command: TeleportLocation" },
                    { 53u, "Command: TeleportName" },
                    { 54u, "Category: House" },
                    { 55u, "Category: HouseDecor" },
                    { 56u, "Command: HouseDecorAdd" },
                    { 57u, "Command: HouseDecorLookup" },
                    { 58u, "Command: HouseTeleport" },
                    { 59u, "Category: Item" },
                    { 60u, "Category: EntityModify" },
                    { 61u, "Command: EntityModifyDisplayInfo" },
                    { 62u, "Category: Movement" },
                    { 63u, "Category: MovementSpline" },
                    { 64u, "Command: MovementSplineAdd" },
                    { 65u, "Command: MovementSplineClear" },
                    { 66u, "Command: MovementSplineLaunch" },
                    { 67u, "Category: MovementGenerator" },
                    { 68u, "Command: MovementGeneratorDirect" },
                    { 69u, "Command: MovementGeneratorRandom" },
                    { 70u, "Category: Path" },
                    { 71u, "Command: PathUnlock" },
                    { 72u, "Command: PathActivate" },
                    { 73u, "Command: PathXP" },
                    { 74u, "Category: Pet" },
                    { 75u, "Command: PetUnlockFlair" },
                    { 76u, "Category: Quest" },
                    { 77u, "Command: QuestAdd" },
                    { 78u, "Command: QuestAchieve" },
                    { 79u, "Command: QuestAchieveObjective" },
                    { 80u, "Command: QuestObjective" },
                    { 81u, "Command: QuestKill" },
                    { 82u, "Category: Realm" },
                    { 83u, "Category: Spell" },
                    { 84u, "Command: SpellAdd" },
                    { 85u, "Command: SpellCast" },
                    { 86u, "Command: SpellResetCooldown" },
                    { 87u, "Category: Title" },
                    { 88u, "Command: TitleAdd" },
                    { 89u, "Command: TitleRevoke" },
                    { 90u, "Command: TitleAll" },
                    { 91u, "Command: TitleNone" },
                    { 92u, "Command: ItemLookup" },
                    { 94u, "Command: RealmMOTD" },
                    { 95u, "Category: Story" },
                    { 96u, "Command: StoryPanel" },
                    { 97u, "Command: StoryCommunicator" },
                    { 98u, "Category: Reputation" },
                    { 99u, "Command: ReputationUpdate" },
                    { 100u, "Category: Guild" },
                    { 101u, "Command: GuildRegister" },
                    { 102u, "Command: GuildJoin" },
                    { 103u, "Category: Map" },
                    { 104u, "Command: MapUnload" },
                    { 105u, "Command: MapPlayerRemove" },
                    { 106u, "Command: MapPlayerRemoveCancel" },
                    { 107u, "Category: RealmShutdown" },
                    { 108u, "Command: RealmShutdownStart" },
                    { 109u, "Command: RealmShutdownCancel" },
                    { 110u, "Command: QuestList" },
                    { 111u, "Command: RealmMaxPlayers" },
                    { 112u, "Category: Script" },
                    { 113u, "Command: ScriptReload" },
                    { 114u, "Command: ScriptInfo" },
                    { 115u, "Command: ScriptAdd" },
                    { 116u, "Command: ItemInfo" },
                    { 117u, "Category: Ban" },
                    { 118u, "Category: BanAccount" },
                    { 119u, "Command: BanAccountPlayer" },
                    { 120u, "Command: BanAccountCharacter" },
                    { 121u, "Category: EntityThreat" },
                    { 122u, "Command: EntityThreatAdjust" },
                    { 123u, "Command: EntityThreatList" },
                    { 124u, "Command: EntityThreatClear" },
                    { 125u, "Command: EntityThreatRemove" },
                    { 10000u, "Other: InstantLogout" },
                    { 10001u, "Other: Signature" },
                    { 10002u, "Other: BypassInstanceLimits" },
                    { 10003u, "Other: GMFlag" },
                    { 10004u, "Other: EntitlementGrantOther" }
                });

            migrationBuilder.InsertData(
                table: "role",
                columns: new[] { "id", "flags", "name" },
                values: new object[,]
                {
                    { 1u, 1u, "Player" },
                    { 2u, 1u, "GameMaster" },
                    { 3u, 2u, "Administrator" },
                    { 4u, 2u, "Console" },
                    { 5u, 2u, "WebSocket" }
                });

            migrationBuilder.InsertData(
                table: "server",
                columns: new[] { "id", "host", "name", "port" },
                values: new object[] { (byte)1, "127.0.0.1", "NexusForever", (ushort)24000 });

            migrationBuilder.InsertData(
                table: "server_message",
                columns: new[] { "index", "language", "message" },
                values: new object[,]
                {
                    { (byte)0, (byte)0, "Welcome to this NexusForever server!\nVisit: https://github.com/NexusForever/NexusForever" },
                    { (byte)0, (byte)1, "Willkommen auf diesem NexusForever server!\nBesuch: https://github.com/NexusForever/NexusForever" }
                });

            migrationBuilder.CreateIndex(
                name: "email",
                table: "account",
                column: "email");

            migrationBuilder.CreateIndex(
                name: "gameToken",
                table: "account",
                column: "gameToken");

            migrationBuilder.CreateIndex(
                name: "sessionKey",
                table: "account",
                column: "sessionKey");

            migrationBuilder.CreateIndex(
                name: "IX_account_permission_permissionId",
                table: "account_permission",
                column: "permissionId");

            migrationBuilder.CreateIndex(
                name: "IX_account_role_roleId",
                table: "account_role",
                column: "roleId");

            migrationBuilder.CreateIndex(
                name: "IX_account_store_purchase_history_accountId",
                table: "account_store_purchase_history",
                column: "accountId");

            migrationBuilder.CreateIndex(
                name: "IX_role_permission_permissionId",
                table: "role_permission",
                column: "permissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_costume_unlock");

            migrationBuilder.DropTable(
                name: "account_credd_history");

            migrationBuilder.DropTable(
                name: "account_credd_order");

            migrationBuilder.DropTable(
                name: "account_currency");

            migrationBuilder.DropTable(
                name: "account_daily_login");

            migrationBuilder.DropTable(
                name: "account_entitlement");

            migrationBuilder.DropTable(
                name: "account_external_reference");

            migrationBuilder.DropTable(
                name: "account_fortune_session");

            migrationBuilder.DropTable(
                name: "account_generic_unlock");

            migrationBuilder.DropTable(
                name: "account_inventory");

            migrationBuilder.DropTable(
                name: "account_item_cooldown");

            migrationBuilder.DropTable(
                name: "account_keybinding");

            migrationBuilder.DropTable(
                name: "account_pending_item");

            migrationBuilder.DropTable(
                name: "account_permission");

            migrationBuilder.DropTable(
                name: "account_reward_rotation_grant");

            migrationBuilder.DropTable(
                name: "account_role");

            migrationBuilder.DropTable(
                name: "account_store_purchase_history");

            migrationBuilder.DropTable(
                name: "account_suspension");

            migrationBuilder.DropTable(
                name: "role_permission");

            migrationBuilder.DropTable(
                name: "server");

            migrationBuilder.DropTable(
                name: "server_message");

            migrationBuilder.DropTable(
                name: "account");

            migrationBuilder.DropTable(
                name: "role");

            migrationBuilder.DropTable(
                name: "permission");
        }
    }
}
