using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class ContractQuestAcceptance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "contractAcceptanceId",
                table: "character_quest",
                type: "char(32)",
                maxLength: 32,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_character_quest_contractAcceptance",
                table: "character_quest",
                columns: new[] { "id", "contractAcceptanceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_character_quest_contractAcceptance",
                table: "character_quest");

            migrationBuilder.DropColumn(
                name: "contractAcceptanceId",
                table: "character_quest");
        }
    }
}
