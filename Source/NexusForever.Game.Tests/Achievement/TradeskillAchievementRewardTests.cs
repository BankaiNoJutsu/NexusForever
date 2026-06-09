using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using NexusForever.Database;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Guild;
using NexusForever.Game.Achievement;
using NexusForever.Game.Static;
using NexusForever.Game.Static.Achievement;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Achievement;

namespace NexusForever.Game.Tests.Achievement;

[Collection(LegacyServiceProviderCollection.Name)]
public class TradeskillAchievementRewardTests
{
    [Fact]
    public void CompleteCraftItemTechTreeAchievement_GrantsRewardSchematicAndEnsuresTalentPoints()
    {
        var globalAchievementManager = new GlobalAchievementManager();
        using var scope = new LegacyServiceProviderScope(BuildProvider(globalAchievementManager, gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Achievement), CreateGameTable(new AchievementEntry
            {
                Id                    = 1471u,
                AchievementTypeId     = (uint)AchievementType.CraftItem,
                AchievementCategoryId = 38u,
                ObjectId              = 14838u,
                RequiredProgress      = 3u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.AchievementChecklist), CreateGameTable<AchievementChecklistEntry>());
            SetTable(gameTableManager, nameof(GameTableManager.AchievementCategory), CreateGameTable(
                new AchievementCategoryEntry
                {
                    Id                          = 38u,
                    AchievementCategoryIdParent = 18u
                },
                new AchievementCategoryEntry
                {
                    Id                          = 18u,
                    AchievementCategoryIdParent = 5u
                }));
            SetTable(gameTableManager, nameof(GameTableManager.Tradeskill), CreateGameTable(new TradeskillEntry
            {
                Id                    = (uint)TradeskillType.Technologist,
                AchievementCategoryId = 18u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.TradeskillAchievementReward), CreateGameTable(new TradeskillAchievementRewardEntry
            {
                Id                       = 162u,
                AchievementId            = 1471u,
                TalentPoints             = 1u,
                TradeSkillSchematicId00  = 2954u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.TradeskillSchematic2), CreateGameTable(new TradeskillSchematic2Entry
            {
                Id           = 2954u,
                TradeSkillId = (uint)TradeskillType.Technologist
            }));
            SetTable(gameTableManager, nameof(GameTableManager.TradeskillTier), CreateGameTable<TradeskillTierEntry>());
        }));
        globalAchievementManager.Initialise();

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy, out RecordingDispatchProxy<IGameSession> sessionProxy);
        var manager = new CharacterAchievementManager(player, new CharacterModel
        {
            Id = 30ul
        });

        manager.CheckAchievements(player, AchievementType.CraftItem, 14838u, count: 3u);

        RecordingDispatchProxy<IPlayer>.Invocation schematicCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.LearnSchematic)));
        Assert.Equal(2954u, schematicCall.Arguments[0]);
        Assert.False((bool)schematicCall.Arguments[1]);

        RecordingDispatchProxy<IPlayer>.Invocation talentCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.EnsureTradeskillTalentPointTotal)));
        Assert.Equal(TradeskillType.Technologist, talentCall.Arguments[0]);
        Assert.Equal(1u, talentCall.Arguments[1]);

        Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)),
            call => call.Arguments[0] is ServerAchievementUpdate);
    }

    [Fact]
    public void CompleteTradeskillTierTechTreeAchievement_EnsuresTalentPointFromTierOwner()
    {
        var globalAchievementManager = new GlobalAchievementManager();
        using var scope = new LegacyServiceProviderScope(BuildProvider(globalAchievementManager, gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Achievement), CreateGameTable(new AchievementEntry
            {
                Id                    = 1470u,
                AchievementTypeId     = (uint)AchievementType.TradeskillTier,
                AchievementCategoryId = 38u,
                ObjectId              = 34u,
                RequiredProgress      = 1u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.AchievementChecklist), CreateGameTable<AchievementChecklistEntry>());
            SetTable(gameTableManager, nameof(GameTableManager.AchievementCategory), CreateGameTable<AchievementCategoryEntry>());
            SetTable(gameTableManager, nameof(GameTableManager.Tradeskill), CreateGameTable<TradeskillEntry>());
            SetTable(gameTableManager, nameof(GameTableManager.TradeskillAchievementReward), CreateGameTable(new TradeskillAchievementRewardEntry
            {
                Id            = 599u,
                AchievementId = 1470u,
                TalentPoints  = 1u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.TradeskillSchematic2), CreateGameTable<TradeskillSchematic2Entry>());
            SetTable(gameTableManager, nameof(GameTableManager.TradeskillTier), CreateGameTable(new TradeskillTierEntry
            {
                Id           = 34u,
                TradeSkillId = (uint)TradeskillType.Technologist
            }));
        }));
        globalAchievementManager.Initialise();

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy, out _);
        var manager = new CharacterAchievementManager(player, new CharacterModel
        {
            Id = 30ul
        });

        manager.CheckAchievements(player, AchievementType.TradeskillTier, 34u);

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.LearnSchematic)));
        RecordingDispatchProxy<IPlayer>.Invocation talentCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.EnsureTradeskillTalentPointTotal)));
        Assert.Equal(TradeskillType.Technologist, talentCall.Arguments[0]);
        Assert.Equal(1u, talentCall.Arguments[1]);
    }

    [Fact]
    public void GrantCompletedTradeskillAchievementRewards_BackfillsAlreadyCompletedTechTreeRewards()
    {
        var globalAchievementManager = new GlobalAchievementManager();
        using var scope = new LegacyServiceProviderScope(BuildProvider(globalAchievementManager, gameTableManager =>
        {
            SetTable(gameTableManager, nameof(GameTableManager.Achievement), CreateGameTable(
                new AchievementEntry
                {
                    Id                    = 1470u,
                    AchievementTypeId     = (uint)AchievementType.TradeskillTier,
                    AchievementCategoryId = 38u,
                    ObjectId              = 34u,
                    RequiredProgress      = 1u
                },
                new AchievementEntry
                {
                    Id                    = 1471u,
                    AchievementTypeId     = (uint)AchievementType.CraftItem,
                    AchievementCategoryId = 38u,
                    ObjectId              = 14838u,
                    RequiredProgress      = 3u
                }));
            SetTable(gameTableManager, nameof(GameTableManager.AchievementChecklist), CreateGameTable<AchievementChecklistEntry>());
            SetTable(gameTableManager, nameof(GameTableManager.AchievementCategory), CreateGameTable(
                new AchievementCategoryEntry
                {
                    Id                          = 38u,
                    AchievementCategoryIdParent = 18u
                },
                new AchievementCategoryEntry
                {
                    Id                          = 18u,
                    AchievementCategoryIdParent = 5u
                }));
            SetTable(gameTableManager, nameof(GameTableManager.Tradeskill), CreateGameTable(new TradeskillEntry
            {
                Id                    = (uint)TradeskillType.Technologist,
                AchievementCategoryId = 18u
            }));
            SetTable(gameTableManager, nameof(GameTableManager.TradeskillAchievementReward), CreateGameTable(
                new TradeskillAchievementRewardEntry
                {
                    Id            = 599u,
                    AchievementId = 1470u,
                    TalentPoints  = 1u
                },
                new TradeskillAchievementRewardEntry
                {
                    Id                      = 162u,
                    AchievementId           = 1471u,
                    TalentPoints            = 1u,
                    TradeSkillSchematicId00 = 2954u
                }));
            SetTable(gameTableManager, nameof(GameTableManager.TradeskillSchematic2), CreateGameTable(new TradeskillSchematic2Entry
            {
                Id           = 2954u,
                TradeSkillId = (uint)TradeskillType.Technologist
            }));
            SetTable(gameTableManager, nameof(GameTableManager.TradeskillTier), CreateGameTable(new TradeskillTierEntry
            {
                Id           = 34u,
                TradeSkillId = (uint)TradeskillType.Technologist
            }));
        }));
        globalAchievementManager.Initialise();

        IPlayer player = CreatePlayer(out RecordingDispatchProxy<IPlayer> playerProxy, out _);
        var manager = new CharacterAchievementManager(player, new CharacterModel
        {
            Id = 30ul,
            Achievement =
            {
                new CharacterAchievementModel
                {
                    Id            = 30ul,
                    AchievementId = 1470,
                    ProgressState = 1u,
                    DateCompleted = DateTime.UtcNow
                },
                new CharacterAchievementModel
                {
                    Id            = 30ul,
                    AchievementId = 1471,
                    ProgressState = 3u,
                    DateCompleted = DateTime.UtcNow
                }
            }
        });

        manager.GrantCompletedTradeskillAchievementRewards(player);

        RecordingDispatchProxy<IPlayer>.Invocation schematicCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.LearnSchematic)));
        Assert.Equal(2954u, schematicCall.Arguments[0]);

        RecordingDispatchProxy<IPlayer>.Invocation talentCall = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.EnsureTradeskillTalentPointTotal)));
        Assert.Equal(TradeskillType.Technologist, talentCall.Arguments[0]);
        Assert.Equal(2u, talentCall.Arguments[1]);
    }

    private static IPlayer CreatePlayer(
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IGameSession> sessionProxy)
    {
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out sessionProxy);
        IGuildManager guildManager = RecordingDispatchProxy<IGuildManager>.Create(out _);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);

        playerProxy.SetProperty(nameof(IPlayer.CharacterId), 30ul);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.GuildManager), guildManager);
        playerProxy.SetProperty(nameof(IPlayer.Name), "Joy Ner");
        playerProxy.SetMethodReturn(nameof(IPlayer.LearnSchematic), true);
        playerProxy.SetMethodReturn(nameof(IPlayer.EnsureTradeskillTalentPointTotal), 1u);

        return player;
    }

    private static IServiceProvider BuildProvider(
        GlobalAchievementManager globalAchievementManager,
        Action<GameTableManager> configure)
    {
        var gameTableManager = (GameTableManager)RuntimeHelpers.GetUninitializedObject(typeof(GameTableManager));
        configure(gameTableManager);

        var disableManager = new DisableManager();
        SetPrivateField(disableManager, "disables", ImmutableDictionary<ulong, Disable>.Empty);

        return new ServiceCollection()
            .AddSingleton(gameTableManager)
            .AddSingleton(CreateEmptyDatabaseManager())
            .AddSingleton(disableManager)
            .AddSingleton(globalAchievementManager)
            .BuildServiceProvider();
    }

    private static DatabaseManager CreateEmptyDatabaseManager()
    {
        var manager = (DatabaseManager)RuntimeHelpers.GetUninitializedObject(typeof(DatabaseManager));
        SetPrivateField(manager, "databases", ImmutableDictionary<Type, IDatabase>.Empty);
        return manager;
    }

    private static GameTable<T> CreateGameTable<T>(params T[] entries) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        SetAutoProperty(table, nameof(GameTable<T>.Entries), entries);
        SetPrivateField(table, "header", new GameTableHeader
        {
            MaxId = entries.Length == 0 ? 0ul : entries.Max(GetEntryId) + 1ul
        });
        SetPrivateField(table, "lookup", BuildLookup(entries));
        return table;
    }

    private static int[] BuildLookup<T>(T[] entries) where T : class, new()
    {
        if (entries.Length == 0)
            return [];

        int[] lookup = Enumerable.Repeat(-1, (int)(entries.Max(GetEntryId) + 1ul)).ToArray();
        for (int i = 0; i < entries.Length; i++)
            lookup[(int)GetEntryId(entries[i])] = i;

        return lookup;
    }

    private static ulong GetEntryId<T>(T entry)
    {
        FieldInfo idField = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public)[0];
        return Convert.ToUInt64(idField.GetValue(entry));
    }

    private static void SetTable<T>(GameTableManager gameTableManager, string propertyName, GameTable<T> table) where T : class, new()
    {
        SetAutoProperty(gameTableManager, propertyName, table);
    }

    private static void SetAutoProperty(object instance, string propertyName, object value)
    {
        FieldInfo backingField = instance.GetType()
            .GetField($"<{propertyName}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
        backingField.SetValue(instance, value);
    }

    private static void SetPrivateField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType()
            .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.SetValue(instance, value);
    }
}
