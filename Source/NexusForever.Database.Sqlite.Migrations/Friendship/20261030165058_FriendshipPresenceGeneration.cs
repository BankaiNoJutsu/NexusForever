using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Friendship
{
    /// <inheritdoc />
    public partial class FriendshipPresenceGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "presenceAdmission",
                table: "account",
                type: "bigint(20)",
                nullable: false,
                defaultValue: 0ul);

            migrationBuilder.AddColumn<ulong>(
                name: "presenceCharacterId",
                table: "account",
                type: "bigint(20)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "presenceClosed",
                table: "account",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<ushort>(
                name: "presenceRealmId",
                table: "account",
                type: "smallint(5)",
                nullable: true);

            migrationBuilder.AddColumn<ulong>(
                name: "presenceSessionEpoch",
                table: "account",
                type: "bigint(20)",
                nullable: false,
                defaultValue: 0ul);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "presenceAdmission",
                table: "account");

            migrationBuilder.DropColumn(
                name: "presenceCharacterId",
                table: "account");

            migrationBuilder.DropColumn(
                name: "presenceClosed",
                table: "account");

            migrationBuilder.DropColumn(
                name: "presenceRealmId",
                table: "account");

            migrationBuilder.DropColumn(
                name: "presenceSessionEpoch",
                table: "account");
        }
    }
}
