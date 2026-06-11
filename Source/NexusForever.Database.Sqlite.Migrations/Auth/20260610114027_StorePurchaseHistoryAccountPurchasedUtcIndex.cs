using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Sqlite.Migrations.Auth
{
    /// <inheritdoc />
    public partial class StorePurchaseHistoryAccountPurchasedUtcIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_account_store_purchase_history_accountId",
                table: "account_store_purchase_history");

            migrationBuilder.CreateIndex(
                name: "accountId_purchasedUtc",
                table: "account_store_purchase_history",
                columns: new[] { "accountId", "purchasedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "accountId_purchasedUtc",
                table: "account_store_purchase_history");

            migrationBuilder.CreateIndex(
                name: "IX_account_store_purchase_history_accountId",
                table: "account_store_purchase_history",
                column: "accountId");
        }
    }
}
