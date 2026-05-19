using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.PlayerPath;
using NexusForever.Network.World.Message.Static;
using NexusForever.Shared;
using Path = NexusForever.Game.Static.PlayerPath.Path;

namespace NexusForever.Game.Tests.Entity;

[Collection(LegacyServiceProviderCollection.Name)]
public class PathManagerTests
{
    [Fact]
    public void AddXp_AwardsInitialPathLevelRewardFromLevelRewardedState()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 2u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 2u, PathXP = 100u }
            ],
            [
                new PathRewardEntry
                {
                    Id = 1u,
                    ObjectId = PathRewardGrant.GetLevelRewardObjectId(Path.Soldier, 1u),
                    Item2Id = 9001u
                }
            ]);

        try
        {
            PathManager manager = CreateManager(
                totalXp: 0u,
                levelRewarded: 0,
                out IPlayer player,
                out var playerProxy,
                out var sessionProxy,
                out var achievementManagerProxy,
                out var inventoryProxy);

            manager.AddXp(1u);

            RecordingDispatchProxy<IInventory>.Invocation inventoryCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(InventoryLocation.Inventory, inventoryCall.Arguments[0]);
            Assert.Equal(9001u, inventoryCall.Arguments[1]);
            Assert.Equal(1u, inventoryCall.Arguments[2]);
            Assert.Equal(ItemUpdateReason.PathReward, inventoryCall.Arguments[3]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation achievementCall =
                Assert.Single(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
            Assert.Same(player, achievementCall.Arguments[0]);
            Assert.Equal(AchievementType.PathLevel, achievementCall.Arguments[1]);
            Assert.Equal((uint)Path.Soldier, achievementCall.Arguments[2]);
            Assert.Equal(0u, achievementCall.Arguments[3]);
            Assert.Equal(1u, achievementCall.Arguments[4]);

            RecordingDispatchProxy<IGameSession>.Invocation sessionCall =
                Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
            var update = Assert.IsType<ServerPathUpdateXP>(sessionCall.Arguments[0]);
            Assert.Equal(1u, update.TotalXP);

            RecordingDispatchProxy<IPlayer>.Invocation castSpellCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
            Assert.Equal(53234u, castSpellCall.Arguments[0]);

            IPathEntry pathEntry = Assert.Single(manager, entry => entry.Path == Path.Soldier);
            Assert.Equal(1u, pathEntry.TotalXp);
            Assert.Equal(1, pathEntry.LevelRewarded);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    [Fact]
    public void AddLevels_AtMaxPathLevelStillAwardsOutstandingCapReward()
    {
        IServiceProvider previousProvider = LegacyServiceProvider.Provider;
        LegacyServiceProvider.Provider = BuildGameTableProvider(
            [
                new PathLevelEntry { Id = 1u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 1u, PathXP = 0u },
                new PathLevelEntry { Id = 30u, PathTypeEnum = (uint)Path.Soldier, PathLevel = 30u, PathXP = 3000u }
            ],
            [
                new PathRewardEntry
                {
                    Id = 30u,
                    ObjectId = PathRewardGrant.GetLevelRewardObjectId(Path.Soldier, 30u),
                    Item2Id = 9030u
                }
            ]);

        try
        {
            PathManager manager = CreateManager(
                totalXp: 3000u,
                levelRewarded: 29,
                out IPlayer player,
                out var playerProxy,
                out var sessionProxy,
                out var achievementManagerProxy,
                out var inventoryProxy);

            manager.AddLevels(1u);

            RecordingDispatchProxy<IInventory>.Invocation inventoryCall = Assert.Single(inventoryProxy.GetInvocations(nameof(IInventory.ItemCreate)));
            Assert.Equal(9030u, inventoryCall.Arguments[1]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation progressCall =
                Assert.Single(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.SetAchievementProgress)));
            Assert.Equal(30u, progressCall.Arguments[4]);

            RecordingDispatchProxy<ICharacterAchievementManager>.Invocation guildCall =
                Assert.Single(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));
            Assert.Same(player, guildCall.Arguments[0]);
            Assert.Equal(AchievementType.GuildMaxPathLevel, guildCall.Arguments[1]);
            Assert.Equal(0u, guildCall.Arguments[2]);

            Assert.Empty(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));

            RecordingDispatchProxy<IPlayer>.Invocation castSpellCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.CastSpell)));
            Assert.Equal(53234u, castSpellCall.Arguments[0]);

            IPathEntry pathEntry = Assert.Single(manager, entry => entry.Path == Path.Soldier);
            Assert.Equal(3000u, pathEntry.TotalXp);
            Assert.Equal(30, pathEntry.LevelRewarded);
        }
        finally
        {
            LegacyServiceProvider.Provider = previousProvider;
        }
    }

    private static PathManager CreateManager(
        uint totalXp,
        byte levelRewarded,
        out IPlayer player,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy,
        out RecordingDispatchProxy<ICharacterAchievementManager> achievementManagerProxy,
        out RecordingDispatchProxy<IInventory> inventoryProxy)
    {
        player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out achievementManagerProxy);
        IInventory inventory = RecordingDispatchProxy<IInventory>.Create(out inventoryProxy);

        playerProxy.SetProperty(nameof(IPlayer.Path), Path.Soldier);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetProperty(nameof(IPlayer.Inventory), inventory);

        return new PathManager(player, new CharacterModel
        {
            Id = 42ul,
            Path =
            [
                new CharacterPathModel
                {
                    Id = 42ul,
                    Path = (byte)Path.Soldier,
                    Unlocked = 1,
                    TotalXp = totalXp,
                    LevelRewarded = levelRewarded
                }
            ]
        });
    }

    private static IServiceProvider BuildGameTableProvider(IEnumerable<PathLevelEntry> pathLevels, IEnumerable<PathRewardEntry> pathRewards)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathLevel), CreateGameTable(pathLevels.ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.PathReward), CreateGameTable(pathRewards.ToArray()));

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .BuildServiceProvider();
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        return table;
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType().GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
        backingField.SetValue(instance, value);
    }
}
