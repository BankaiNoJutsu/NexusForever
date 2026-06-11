using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Auth.Migrations
{
    /// <inheritdoc />
    public partial class StorePurchaseHistoryAccountPurchasedUtcIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "accountId_purchasedUtc",
                table: "account_store_purchase_history",
                columns: new[] { "accountId", "purchasedUtc" });

            DropIndexIfExists(migrationBuilder, "IX_account_store_purchase_history_accountId");
            DropIndexIfExists(migrationBuilder, "FK__account_store_purchase_history_accountId__account_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_account_store_purchase_history_accountId",
                table: "account_store_purchase_history",
                column: "accountId");

            migrationBuilder.DropIndex(
                name: "accountId_purchasedUtc",
                table: "account_store_purchase_history");
        }

        private static void DropIndexIfExists(MigrationBuilder migrationBuilder, string indexName)
        {
            migrationBuilder.Sql($"""
                SET @drop_account_store_purchase_history_index = (
                    SELECT IF(
                        COUNT(*) > 0,
                        'ALTER TABLE `account_store_purchase_history` DROP INDEX `{indexName}`',
                        'SELECT 1')
                    FROM information_schema.statistics
                    WHERE table_schema = DATABASE()
                        AND table_name = 'account_store_purchase_history'
                        AND index_name = '{indexName}');
                """);
            migrationBuilder.Sql("PREPARE drop_account_store_purchase_history_index_stmt FROM @drop_account_store_purchase_history_index;");
            migrationBuilder.Sql("EXECUTE drop_account_store_purchase_history_index_stmt;");
            migrationBuilder.Sql("DEALLOCATE PREPARE drop_account_store_purchase_history_index_stmt;");
        }
    }
}
