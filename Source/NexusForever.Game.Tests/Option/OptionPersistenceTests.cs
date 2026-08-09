using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Movement;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Matching.Match;
using NexusForever.Game.Abstract.Matching.Queue;
using NexusForever.Game.Abstract.Reputation;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Option;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.Network.Internal;
using Microting.EntityFrameworkCore.MySql.Infrastructure;

namespace NexusForever.Game.Tests.Option;

public class OptionPersistenceTests
{
    [Fact]
    public void CharacterContext_MapsCharacterOptionColumns()
    {
        using CharacterContext context = CreateContext();
        IEntityType entityType = context.Model.FindEntityType(typeof(CharacterModel))!;
        StoreObjectIdentifier table = StoreObjectIdentifier.Table("character", null);

        AssertColumn<byte>(entityType, table, nameof(CharacterModel.CastingOptions), "castingOptions", "tinyint(3) unsigned", (byte)0);
        AssertColumn<bool>(entityType, table, nameof(CharacterModel.SharedChallengeEnabled), "sharedChallengeEnabled", "tinyint(1)", false);
        AssertColumn<bool>(entityType, table, nameof(CharacterModel.DisableOtherPlayersCombatLogs), "disableOtherPlayersCombatLogs", "tinyint(1)", false);
        AssertColumn<ushort>(entityType, table, nameof(CharacterModel.CombatLogDisableFlags), "combatLogDisableFlags", "smallint(5) unsigned", (ushort)0);
    }

    [Fact]
    public void PlayerSave_WithOptionChanges_PersistsCharacterOptionColumns()
    {
        using CharacterContext context = CreateContext();
        Player player = CreatePlayer(123ul);

        player.CastingOptions = CastingOptionFlags.ButtonDownForAbilities | CastingOptionFlags.HoldToContinueCasting;
        player.SharedChallengeEnabled = true;
        player.DisableOtherPlayersCombatLogs = true;
        player.CombatLogDisableFlags = CombatLogOptions.DisableDamage | CombatLogOptions.DisableHeal;

        player.Save(context);

        EntityEntry<CharacterModel> entry = Assert.Single(context.ChangeTracker.Entries<CharacterModel>());
        Assert.Equal(123ul, entry.Entity.Id);
        Assert.Equal((byte)(CastingOptionFlags.ButtonDownForAbilities | CastingOptionFlags.HoldToContinueCasting), entry.Entity.CastingOptions);
        Assert.True(entry.Entity.SharedChallengeEnabled);
        Assert.True(entry.Entity.DisableOtherPlayersCombatLogs);
        Assert.Equal((ushort)(CombatLogOptions.DisableDamage | CombatLogOptions.DisableHeal), entry.Entity.CombatLogDisableFlags);

        Assert.True(entry.Property(p => p.CastingOptions).IsModified);
        Assert.True(entry.Property(p => p.SharedChallengeEnabled).IsModified);
        Assert.True(entry.Property(p => p.DisableOtherPlayersCombatLogs).IsModified);
        Assert.True(entry.Property(p => p.CombatLogDisableFlags).IsModified);
    }

    private static void AssertColumn<T>(
        IEntityType entityType,
        StoreObjectIdentifier table,
        string propertyName,
        string columnName,
        string columnType,
        T defaultValue)
    {
        IProperty property = entityType.FindProperty(propertyName)!;

        Assert.Equal(columnName, property.GetColumnName(table));
        Assert.Equal(columnType, property.GetColumnType());
        Assert.Equal(defaultValue, property.GetDefaultValue());
    }

    private static CharacterContext CreateContext()
    {
        DbContextOptions<CharacterContext> options = new DbContextOptionsBuilder<CharacterContext>()
            .UseMySql(
                "Server=127.0.0.1;Database=nexus_forever_character;User ID=nexus_forever;Password=nexus_forever;",
                new MySqlServerVersion(new Version(8, 0, 0)))
            .Options;

        return new CharacterContext(options);
    }

    private static Player CreatePlayer(ulong characterId)
    {
        ICurrencyManager currencyManager = CreateProxy<ICurrencyManager>();
        var player = new Player(
            CreateProxy<IMovementManager>(),
            CreateProxy<IInternalMessagePublisher>(),
            CreateProxy<IEntityFactory>(),
            CreateProxy<IMatchingManager>(),
            CreateProxy<IMatchManager>(),
            CreateProxy<IGameTableManager>(),
            currencyManager,
            guildManager: CreateProxy<IGuildManager>());

        SetPrivateProperty(player, nameof(Player.Identity), new Identity
        {
            Id = characterId,
            RealmId = 1
        });
        SetPrivateProperty(player, nameof(Player.Inventory), CreateProxy<IInventory>());
        SetPrivateProperty(player, nameof(Player.PathManager), CreateProxy<IPathManager>());
        SetPrivateProperty(player, nameof(Player.TitleManager), CreateProxy<ITitleManager>());
        SetPrivateProperty(player, nameof(Player.CostumeManager), CreateProxy<ICostumeManager>());
        SetPrivateProperty(player, nameof(Player.PetCustomisationManager), CreateProxy<IPetCustomisationManager>());
        SetPrivateProperty(player, nameof(Player.KeybindingManager), CreateProxy<ICharacterKeybindingManager>());
        SetPrivateProperty(player, nameof(Player.SpellManager), CreateProxy<ISpellManager>());
        SetPrivateProperty(player, nameof(Player.DatacubeManager), CreateProxy<IDatacubeManager>());
        SetPrivateProperty(player, nameof(Player.GalacticArchiveManager), CreateProxy<IGalacticArchiveManager>());
        SetPrivateProperty(player, nameof(Player.MailManager), CreateProxy<IMailManager>());
        SetPrivateProperty(player, nameof(Player.ZoneMapManager), CreateProxy<IZoneMapManager>());
        SetPrivateProperty(player, nameof(Player.QuestManager), CreateProxy<IQuestManager>());
        SetPrivateProperty(player, nameof(Player.AchievementManager), CreateProxy<ICharacterAchievementManager>());
        SetPrivateProperty(player, nameof(Player.SupplySatchelManager), CreateProxy<ISupplySatchelManager>());
        SetPrivateProperty(player, nameof(Player.XpManager), CreateProxy<IXpManager>());
        SetPrivateProperty(player, nameof(Player.ReputationManager), CreateProxy<IReputationManager>());
        SetPrivateProperty(player, nameof(Player.EntitlementManager), CreateProxy<ICharacterEntitlementManager>());
        SetPrivateProperty(player, nameof(Player.AppearanceManager), CreateProxy<IAppearanceManager>());

        return player;
    }

    private static T CreateProxy<T>() where T : class
    {
        return RecordingDispatchProxy<T>.Create(out _);
    }

    private static void SetPrivateProperty<T>(object instance, string propertyName, T value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName)!;
        property.GetSetMethod(true)!.Invoke(instance, [value]);
    }

}
