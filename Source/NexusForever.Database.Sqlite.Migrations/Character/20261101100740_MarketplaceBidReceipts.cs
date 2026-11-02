using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Character
{
    /// <inheritdoc />
    public partial class MarketplaceBidReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "marketplace_bid_receipt",
                columns: table => new
                {
                    id = table.Column<string>(type: "char(32)", maxLength: 32, nullable: false),
                    payload = table.Column<string>(type: "longtext", nullable: false),
                    createdUtc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "marketplace_bid_receipt");
        }
    }
}
