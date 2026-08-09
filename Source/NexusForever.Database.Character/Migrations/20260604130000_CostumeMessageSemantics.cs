using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NexusForever.Database.Character.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(CharacterContext))]
    [Migration("20260604130000_CostumeMessageSemantics")]
    public partial class CostumeMessageSemantics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 20260516125523_CostumeMessageChanges already applied these schema
            // changes. Keep this published migration ID as a no-op so both fresh
            // deployments and databases with the earlier history can upgrade.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The preceding migration owns the reverse operation.
        }
    }
}
