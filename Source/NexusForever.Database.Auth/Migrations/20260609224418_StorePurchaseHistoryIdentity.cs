using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
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
                type: "bigint(20) unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint(20) unsigned")
                .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<ulong>(
                name: "id",
                table: "account_store_purchase_history",
                type: "bigint(20) unsigned",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "bigint(20) unsigned")
                .OldAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);
        }
    }
}
