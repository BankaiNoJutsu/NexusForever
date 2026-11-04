using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NexusForever.Database.Character;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Abilities;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using Factory = NexusForever.Database.Sqlite.Migrations.Design.CharacterContextFactory;

namespace NexusForever.Game.Tests.Database;

public sealed class CharacterBonusAmpPowerMigrationTests
{
    [Theory]
    [InlineData(false, "smallint(5) unsigned")]
    [InlineData(true, "smallint(5)")]
    public void MigrationAddsOnlyTheZeroDefaultBonusPowerColumn(bool sqlite, string columnType)
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        using var context = new CharacterContext(sqlite ? Options(connection) : CharacterMigrationHistoryTests.CreateScriptOptions());
        Assert.False(context.Database.HasPendingModelChanges());
        var assembly = context.GetService<IMigrationsAssembly>();
        var migrationType = Assert.Single(assembly.Migrations, row => row.Key.EndsWith("_CharacterBonusAmpPower", StringComparison.Ordinal)).Value;
        var migration = assembly.CreateMigration(migrationType, context.Database.ProviderName!);
        var add = Assert.IsType<AddColumnOperation>(Assert.Single(migration.UpOperations));
        Assert.Equal("character", add.Table);
        Assert.Equal("bonusAmpPower", add.Name);
        Assert.Equal(columnType, add.ColumnType);
        Assert.False(add.IsNullable);
        Assert.Equal((ushort)0, Assert.IsType<ushort>(add.DefaultValue));
        Assert.Equal(add.Name, Assert.IsType<DropColumnOperation>(Assert.Single(migration.DownOperations)).Name);
    }

    [Fact]
    public async Task CopiedLegacyCharacterKeepsExistingProgressAndStartsWithZeroBonusPower()
    {
        await using var original = new SqliteConnection("Data Source=:memory:");
        await using var copy = new SqliteConnection("Data Source=:memory:");
        await original.OpenAsync();
        await copy.OpenAsync();
        await using var before = new CharacterContext(Options(original));
        await before.GetService<IMigrator>().MigrateAsync("20261103170302_RealmFirstClaims");
        await before.Database.ExecuteSqlRawAsync("INSERT INTO character (id,name,isOnline,totalXp,activeSpec,bonusAbilityTierPoints) VALUES (42,'Legacy',0,123,2,1)");
        var nodes = Enumerable.Range(300, 10).Select(id => new EldanAugmentationEntry { Id = (uint)id, PowerCost = 5 }).ToArray();
        foreach (var node in nodes)
            await before.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO character_action_set_amp (id,specIndex,ampId) VALUES (42,2,{node.Id})");
        original.BackupDatabase(copy);

        await using var upgraded = new CharacterContext(Options(copy));
        await upgraded.Database.MigrateAsync();
        await upgraded.Database.MigrateAsync();
        Assert.Empty(await upgraded.Database.GetPendingMigrationsAsync());
        var character = await upgraded.Character.Include(row => row.ActionSetAmp).SingleAsync();
        Assert.Equal(42ul, character.Id);
        Assert.Equal("Legacy", character.Name);
        Assert.Equal(123u, character.TotalXp);
        Assert.Equal((byte)2, character.ActiveSpec);
        Assert.Equal((byte)1, character.BonusAbilityTierPoints);
        Assert.Equal((ushort)0, character.BonusAmpPower);
        Assert.Empty(await before.Database.SqlQueryRaw<string>("SELECT name AS Value FROM pragma_table_info('character') WHERE name = 'bonusAmpPower'").ToArrayAsync());

        var player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.EldanAugmentation), PlayerBindPointTests.Table(nodes));
        var spells = new SpellManager(player, character, gameTableManager: tables);
        var oldSpec = spells.GetActionSet(2);
        Assert.Equal(10, oldSpec.Amps.Count());
        Assert.Equal((byte)45, spells.GetActionSet(0).AmpPoints);
        Assert.Equal((byte)0, oldSpec.AmpPoints);
        Assert.Throws<OverflowException>(() => oldSpec.AddAmp(new EldanAugmentationEntry { Id = 310, PowerCost = 1 }));
        oldSpec.RemoveAmp(AmpRespecType.Single, 300);
        Assert.Equal((byte)0, oldSpec.AmpPoints);
        oldSpec.RemoveAmp(AmpRespecType.Full, 0);
        Assert.Equal((byte)45, oldSpec.AmpPoints);
    }

    private static DbContextOptions<CharacterContext> Options(SqliteConnection connection)
        => new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite(connection, builder => builder.MigrationsAssembly(typeof(Factory).Assembly.GetName().Name)).Options;
}
