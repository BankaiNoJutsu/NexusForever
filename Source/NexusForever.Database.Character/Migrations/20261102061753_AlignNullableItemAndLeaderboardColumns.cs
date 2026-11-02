using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class AlignNullableItemAndLeaderboardColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Match the existing runtime/EnsureCreated contract without rewriting
            // retained codec bytes or optional cached names.
            MakeNullable(migrationBuilder, "item", "microchipIds", "varchar(128)");
            MakeNullable(migrationBuilder, "item", "runeSlots", "varchar(256)");
            MakeNullable(migrationBuilder, "leaderboard_pve_score", "playerName", "varchar(64)");
            MakeNullable(migrationBuilder, "leaderboard_pvp_score", "playerName", "varchar(64)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new System.NotSupportedException("Nullable persisted values cannot be narrowed without an explicit data conversion policy.");
        }

        private static void MakeNullable(MigrationBuilder migrationBuilder, string table, string name, string type)
            => migrationBuilder.AlterColumn<string>(name: name, table: table, type: type, nullable: true,
                    defaultValue: "", oldClrType: typeof(string), oldType: type, oldNullable: false, oldDefaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4").OldAnnotation("MySql:CharSet", "utf8mb4");
    }
}
