using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Group
{
    /// <inheritdoc />
    public partial class GroupSqliteInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "character",
                columns: table => new
                {
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    realmName = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    sex = table.Column<byte>(type: "INTEGER", nullable: false),
                    race = table.Column<byte>(type: "INTEGER", nullable: false),
                    @class = table.Column<byte>(name: "class", type: "INTEGER", nullable: false),
                    path = table.Column<byte>(type: "INTEGER", nullable: false),
                    faction = table.Column<uint>(type: "INTEGER", nullable: false),
                    currentRealm = table.Column<ushort>(type: "INTEGER", nullable: false),
                    worldZoneId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    mapId = table.Column<uint>(type: "INTEGER", nullable: false),
                    positionX = table.Column<float>(type: "REAL", nullable: false),
                    positionY = table.Column<float>(type: "REAL", nullable: false),
                    positionZ = table.Column<float>(type: "REAL", nullable: false),
                    statsDirty = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    realmDirty = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.characterId, x.realmId });
                });

            migrationBuilder.CreateTable(
                name: "group",
                columns: table => new
                {
                    groupId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    flags = table.Column<uint>(type: "INTEGER", nullable: false),
                    lootRule = table.Column<byte>(type: "INTEGER", nullable: false),
                    lootRuleThreshold = table.Column<byte>(type: "INTEGER", nullable: false),
                    lootThreshold = table.Column<byte>(type: "INTEGER", nullable: false),
                    lootRuleHarvest = table.Column<byte>(type: "INTEGER", nullable: false),
                    match = table.Column<Guid>(type: "TEXT", nullable: true),
                    matchTeam = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.groupId);
                });

            migrationBuilder.CreateTable(
                name: "internal_message",
                columns: table => new
                {
                    messageId = table.Column<Guid>(type: "TEXT", nullable: false),
                    createdAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    processedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    type = table.Column<string>(type: "TEXT", nullable: false),
                    data = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.messageId);
                });

            migrationBuilder.CreateTable(
                name: "character_property",
                columns: table => new
                {
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    property = table.Column<byte>(type: "INTEGER", nullable: false),
                    value = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.characterId, x.realmId, x.property });
                    table.ForeignKey(
                        name: "FK_character_property_character_characterId_realmId",
                        columns: x => new { x.characterId, x.realmId },
                        principalTable: "character",
                        principalColumns: new[] { "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_stat",
                columns: table => new
                {
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    stat = table.Column<byte>(type: "INTEGER", nullable: false),
                    value = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.characterId, x.realmId, x.stat });
                    table.ForeignKey(
                        name: "FK_character_stat_character_characterId_realmId",
                        columns: x => new { x.characterId, x.realmId },
                        principalTable: "character",
                        principalColumns: new[] { "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_invite",
                columns: table => new
                {
                    groupId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    inviteeCharacterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    inviteeRealmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    InviterCharacterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    inviterRealmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    expiration = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.groupId, x.inviteeCharacterId, x.inviteeRealmId });
                    table.ForeignKey(
                        name: "FK_group_invite_character_inviteeCharacterId_inviteeRealmId",
                        columns: x => new { x.inviteeCharacterId, x.inviteeRealmId },
                        principalTable: "character",
                        principalColumns: new[] { "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_invite_group_groupId",
                        column: x => x.groupId,
                        principalTable: "group",
                        principalColumn: "groupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_marker",
                columns: table => new
                {
                    groupId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    marker = table.Column<int>(type: "INTEGER", nullable: false),
                    unitId = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.groupId, x.marker });
                    table.ForeignKey(
                        name: "FK_group_marker_group_groupId",
                        column: x => x.groupId,
                        principalTable: "group",
                        principalColumn: "groupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_member",
                columns: table => new
                {
                    groupId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    index = table.Column<uint>(type: "INTEGER", nullable: false),
                    flags = table.Column<uint>(type: "INTEGER", nullable: false),
                    positionDirty = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => new { x.groupId, x.characterId, x.realmId });
                    table.ForeignKey(
                        name: "FK_group_member_character_characterId_realmId",
                        columns: x => new { x.characterId, x.realmId },
                        principalTable: "character",
                        principalColumns: new[] { "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_member_group_groupId",
                        column: x => x.groupId,
                        principalTable: "group",
                        principalColumn: "groupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_request",
                columns: table => new
                {
                    groupId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    requesterCharacterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    requesterRealmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    requesteeCharacterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    requesteeRealmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    requestType = table.Column<int>(type: "INTEGER", nullable: false),
                    expiration = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.groupId);
                    table.ForeignKey(
                        name: "FK_group_request_character_requesteeCharacterId_requesteeRealmId",
                        columns: x => new { x.requesteeCharacterId, x.requesteeRealmId },
                        principalTable: "character",
                        principalColumns: new[] { "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_request_character_requesterCharacterId_requesterRealmId",
                        columns: x => new { x.requesterCharacterId, x.requesterRealmId },
                        principalTable: "character",
                        principalColumns: new[] { "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_request_group_groupId",
                        column: x => x.groupId,
                        principalTable: "group",
                        principalColumn: "groupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_group",
                columns: table => new
                {
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    groupId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    index = table.Column<uint>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_group", x => new { x.characterId, x.realmId, x.groupId });
                    table.CheckConstraint("CK_character_group_onlytwogroups", "`index` < 2");
                    table.ForeignKey(
                        name: "FK_character_group_character_characterId_realmId",
                        columns: x => new { x.characterId, x.realmId },
                        principalTable: "character",
                        principalColumns: new[] { "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_character_group_group_member_groupId_characterId_realmId",
                        columns: x => new { x.groupId, x.characterId, x.realmId },
                        principalTable: "group_member",
                        principalColumns: new[] { "groupId", "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_leader",
                columns: table => new
                {
                    groupId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.groupId);
                    table.ForeignKey(
                        name: "FK_group_leader_group_groupId",
                        column: x => x.groupId,
                        principalTable: "group",
                        principalColumn: "groupId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_group_leader_group_member_groupId_characterId_realmId",
                        columns: x => new { x.groupId, x.characterId, x.realmId },
                        principalTable: "group_member",
                        principalColumns: new[] { "groupId", "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_group_characterId_realmId",
                table: "character_group",
                columns: new[] { "characterId", "realmId" });

            migrationBuilder.CreateIndex(
                name: "IX_character_group_characterId_realmId_index",
                table: "character_group",
                columns: new[] { "characterId", "realmId", "index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_character_group_groupId_characterId_realmId",
                table: "character_group",
                columns: new[] { "groupId", "characterId", "realmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_character_property_characterId_realmId",
                table: "character_property",
                columns: new[] { "characterId", "realmId" });

            migrationBuilder.CreateIndex(
                name: "IX_character_stat_characterId_realmId",
                table: "character_stat",
                columns: new[] { "characterId", "realmId" });

            migrationBuilder.CreateIndex(
                name: "IX_group_match_matchTeam",
                table: "group",
                columns: new[] { "match", "matchTeam" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_invite_groupId",
                table: "group_invite",
                column: "groupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_invite_inviteeCharacterId_inviteeRealmId",
                table: "group_invite",
                columns: new[] { "inviteeCharacterId", "inviteeRealmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_leader_groupId_characterId_realmId",
                table: "group_leader",
                columns: new[] { "groupId", "characterId", "realmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_marker_groupId",
                table: "group_marker",
                column: "groupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_marker_groupId_unitId",
                table: "group_marker",
                columns: new[] { "groupId", "unitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_member_characterId_realmId",
                table: "group_member",
                columns: new[] { "characterId", "realmId" });

            migrationBuilder.CreateIndex(
                name: "IX_group_member_groupId",
                table: "group_member",
                column: "groupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_member_groupId_index",
                table: "group_member",
                columns: new[] { "groupId", "index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_request_requesteeCharacterId_requesteeRealmId",
                table: "group_request",
                columns: new[] { "requesteeCharacterId", "requesteeRealmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_request_requesterCharacterId_requesterRealmId",
                table: "group_request",
                columns: new[] { "requesterCharacterId", "requesterRealmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_internal_message_processedAt",
                table: "internal_message",
                column: "processedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "character_group");

            migrationBuilder.DropTable(
                name: "character_property");

            migrationBuilder.DropTable(
                name: "character_stat");

            migrationBuilder.DropTable(
                name: "group_invite");

            migrationBuilder.DropTable(
                name: "group_leader");

            migrationBuilder.DropTable(
                name: "group_marker");

            migrationBuilder.DropTable(
                name: "group_request");

            migrationBuilder.DropTable(
                name: "internal_message");

            migrationBuilder.DropTable(
                name: "group_member");

            migrationBuilder.DropTable(
                name: "character");

            migrationBuilder.DropTable(
                name: "group");
        }
    }
}
