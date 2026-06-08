using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Chat
{
    /// <inheritdoc />
    public partial class ChatSqliteInitial : Migration
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
                    realmName = table.Column<string>(type: "TEXT", nullable: true),
                    name = table.Column<string>(type: "TEXT", nullable: true),
                    faction = table.Column<uint>(type: "INTEGER", nullable: false),
                    isOnline = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character", x => new { x.characterId, x.realmId });
                });

            migrationBuilder.CreateTable(
                name: "chat_channel",
                columns: table => new
                {
                    chatId = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    type = table.Column<int>(type: "INTEGER", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: true),
                    password = table.Column<string>(type: "TEXT", nullable: true),
                    referenceType = table.Column<byte>(type: "INTEGER", nullable: true),
                    referenceValue = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_channel", x => x.chatId);
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
                name: "chat_channel_member",
                columns: table => new
                {
                    chatId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    flags = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_channel_member", x => new { x.chatId, x.characterId, x.realmId });
                    table.ForeignKey(
                        name: "FK_chat_channel_member_character_characterId_realmId",
                        columns: x => new { x.characterId, x.realmId },
                        principalTable: "character",
                        principalColumns: new[] { "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_channel_member_chat_channel_chatId",
                        column: x => x.chatId,
                        principalTable: "chat_channel",
                        principalColumn: "chatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "character_chat_channel",
                columns: table => new
                {
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false),
                    chatId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_character_chat_channel", x => new { x.characterId, x.realmId, x.chatId });
                    table.ForeignKey(
                        name: "FK_character_chat_channel_character_characterId_realmId",
                        columns: x => new { x.characterId, x.realmId },
                        principalTable: "character",
                        principalColumns: new[] { "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_character_chat_channel_chat_channel_member_chatId_characterId_realmId",
                        columns: x => new { x.chatId, x.characterId, x.realmId },
                        principalTable: "chat_channel_member",
                        principalColumns: new[] { "chatId", "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_channel_owner",
                columns: table => new
                {
                    chatId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    characterId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    realmId = table.Column<ushort>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_channel_owner", x => x.chatId);
                    table.ForeignKey(
                        name: "FK_chat_channel_owner_chat_channel_chatId",
                        column: x => x.chatId,
                        principalTable: "chat_channel",
                        principalColumn: "chatId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_chat_channel_owner_chat_channel_member_chatId_characterId_realmId",
                        columns: x => new { x.chatId, x.characterId, x.realmId },
                        principalTable: "chat_channel_member",
                        principalColumns: new[] { "chatId", "characterId", "realmId" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_character_chat_channel_chatId_characterId_realmId",
                table: "character_chat_channel",
                columns: new[] { "chatId", "characterId", "realmId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_channel_type_name",
                table: "chat_channel",
                columns: new[] { "type", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_channel_type_referenceType_referenceValue",
                table: "chat_channel",
                columns: new[] { "type", "referenceType", "referenceValue" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_channel_member_characterId_realmId",
                table: "chat_channel_member",
                columns: new[] { "characterId", "realmId" });

            migrationBuilder.CreateIndex(
                name: "IX_chat_channel_owner_chatId_characterId_realmId",
                table: "chat_channel_owner",
                columns: new[] { "chatId", "characterId", "realmId" },
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
                name: "character_chat_channel");

            migrationBuilder.DropTable(
                name: "chat_channel_owner");

            migrationBuilder.DropTable(
                name: "internal_message");

            migrationBuilder.DropTable(
                name: "chat_channel_member");

            migrationBuilder.DropTable(
                name: "character");

            migrationBuilder.DropTable(
                name: "chat_channel");
        }
    }
}
