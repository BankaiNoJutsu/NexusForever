using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class RealmBankItemCodecs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "microchipIds",
                table: "realm_bank_item",
                type: "varchar(128)",
                nullable: true,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "runeSlots",
                table: "realm_bank_item",
                type: "varchar(256)",
                nullable: true,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "microchipIds",
                table: "realm_bank_item");

            migrationBuilder.DropColumn(
                name: "runeSlots",
                table: "realm_bank_item");
        }
    }
}
