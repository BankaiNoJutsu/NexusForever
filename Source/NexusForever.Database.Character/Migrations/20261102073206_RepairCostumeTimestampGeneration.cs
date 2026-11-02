using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    public partial class RepairCostumeTimestampGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing snapshots already describe update generation, but the
            // original table migration only installed the insert default.
            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                table: "character_costume",
                type: "datetime",
                nullable: false,
                defaultValueSql: "current_timestamp()",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "current_timestamp()")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "timestamp",
                table: "character_costume",
                type: "datetime",
                nullable: false,
                defaultValueSql: "current_timestamp()",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldDefaultValueSql: "current_timestamp()")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.None)
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn);
        }
    }
}
