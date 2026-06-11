using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Auth
{
    /// <inheritdoc />
    public partial class StorePurchaseHistoryIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ulong>(
                name: "id",
                table: "account_store_purchase_history",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint(20)")
                .Annotation("Sqlite:Autoincrement", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ulong>(
                name: "id",
                table: "account_store_purchase_history",
                type: "bigint(20)",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);
        }
    }
}
