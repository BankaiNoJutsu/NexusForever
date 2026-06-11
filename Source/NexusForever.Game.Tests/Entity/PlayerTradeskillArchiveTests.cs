using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.Crafting;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Network.World.Message.Model.Crafting;
using NexusForever.Network.World.Message.Model.Shared;
using PlayerIdentity = NexusForever.Game.Abstract.Identity;

namespace NexusForever.Game.Tests.Entity;

public class PlayerTradeskillArchiveTests
{
    [Fact]
    public void LearnTradeskill_DroppingSkillKeepsInactiveArchivedProgress()
    {
        GameTableManager gameTableManager = CreateGameTableManager([]);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out _);
        var player = CreatePlayer(session, questManager, gameTableManager: gameTableManager);

        AddTradeskillState(player, new CharacterTradeskillModel
        {
            Id = 42ul,
            TradeskillId = (uint)TradeskillType.Armorer,
            TradeskillXp = 180u,
            IsActive = 1u,
            TalentPoints = 7u,
            TalentTier00 = 77u
        });

        bool learned = player.LearnTradeskill(TradeskillType.Weaponsmith, TradeskillType.Armorer);

        Assert.True(learned);

        IReadOnlyDictionary<TradeskillType, CharacterTradeskillModel> models = GetTradeskillModels(player);
        CharacterTradeskillModel archived = models[TradeskillType.Armorer];
        Assert.Equal(0u, archived.IsActive);
        Assert.Equal(180u, archived.TradeskillXp);
        Assert.Equal(7u, archived.TalentPoints);
        Assert.Equal(77u, archived.TalentTier00);

        CharacterTradeskillModel learnedTradeskill = models[TradeskillType.Weaponsmith];
        Assert.Equal(1u, learnedTradeskill.IsActive);
        Assert.Equal(0u, learnedTradeskill.TalentPoints);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> calls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.Collection(calls,
            call =>
            {
                var update = Assert.IsType<ServerProfessionUpdate>(call.Arguments[0]);
                Assert.Equal(TradeskillType.Armorer, update.Tradeskill.TradeskillId);
                Assert.Equal(0u, update.Tradeskill.IsActive);
            },
            call =>
            {
                var update = Assert.IsType<ServerProfessionUpdate>(call.Arguments[0]);
                Assert.Equal(TradeskillType.Weaponsmith, update.Tradeskill.TradeskillId);
                Assert.Equal(1u, update.Tradeskill.IsActive);
                Assert.Equal(0u, update.Tradeskill.TalentPoints);
            });
    }

    [Fact]
    public void LearnTradeskill_ReactivatingArchivedSkillDoesNotReawardTierAchievements()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            [
                new TradeskillTierEntry
                {
                    Id = 501u,
                    TradeSkillId = (uint)TradeskillType.Armorer,
                    RequiredXp = 100u
                }
            ]);

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out _);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out var achievementManagerProxy);
        var player = CreatePlayer(session, questManager, achievementManager, gameTableManager);

        AddTradeskillState(player, new CharacterTradeskillModel
        {
            Id = 42ul,
            TradeskillId = (uint)TradeskillType.Armorer,
            TradeskillXp = 180u,
            IsActive = 0u,
            TalentPoints = 6u,
            TalentTier00 = 77u
        });
        AddTradeskillState(player, new CharacterTradeskillModel
        {
            Id = 42ul,
            TradeskillId = (uint)TradeskillType.Weaponsmith,
            TradeskillXp = 25u,
            IsActive = 1u
        });

        bool learned = player.LearnTradeskill(TradeskillType.Armorer, TradeskillType.Weaponsmith);

        Assert.True(learned);
        Assert.Empty(achievementManagerProxy.GetInvocations(nameof(ICharacterAchievementManager.CheckAchievements)));

        IReadOnlyDictionary<TradeskillType, CharacterTradeskillModel> models = GetTradeskillModels(player);
        CharacterTradeskillModel reactivated = models[TradeskillType.Armorer];
        Assert.Equal(1u, reactivated.IsActive);
        Assert.Equal(180u, reactivated.TradeskillXp);
        Assert.Equal(6u, reactivated.TalentPoints);
        Assert.Equal(77u, reactivated.TalentTier00);

        CharacterTradeskillModel archived = models[TradeskillType.Weaponsmith];
        Assert.Equal(0u, archived.IsActive);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> calls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.Collection(calls,
            call =>
            {
                var update = Assert.IsType<ServerProfessionUpdate>(call.Arguments[0]);
                Assert.Equal(TradeskillType.Weaponsmith, update.Tradeskill.TradeskillId);
                Assert.Equal(0u, update.Tradeskill.IsActive);
            },
            call =>
            {
                var update = Assert.IsType<ServerProfessionUpdate>(call.Arguments[0]);
                Assert.Equal(TradeskillType.Armorer, update.Tradeskill.TradeskillId);
                Assert.Equal(1u, update.Tradeskill.IsActive);
                Assert.Equal(180u, update.Tradeskill.TradeskillXp);
            });
    }

    [Fact]
    public void SendTradeskillInitialPackets_IncludesInactiveArchivedTradeskillAndSchematics()
    {
        GameTableManager gameTableManager = CreateGameTableManager(
            [],
            new TradeskillSchematic2Entry
            {
                Id = 7001u,
                TradeSkillId = (uint)TradeskillType.Armorer
            });

        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        var player = CreatePlayer(session, gameTableManager: gameTableManager);

        AddTradeskillState(player, new CharacterTradeskillModel
        {
            Id = 42ul,
            TradeskillId = (uint)TradeskillType.Armorer,
            TradeskillXp = 180u,
            IsActive = 0u,
            TalentPoints = 7u,
            TalentTier00 = 77u
        });
        AddSchematicState(player, new CharacterSchematicModel
        {
            Id = 42ul,
            TradeskillSchematic2Id = 7001u,
            Discovered = true,
            DiscoveryCoordinateX = 1.5f,
            DiscoveryCoordinateY = 2.5f
        });

        player.SendTradeskillInitialPackets();

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> calls = sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        Assert.Collection(calls,
            call =>
            {
                var load = Assert.IsType<ServerProfessionsLoad>(call.Arguments[0]);
                TradeskillInfo tradeskill = Assert.Single(load.Tradeskills);
                Assert.Equal(TradeskillType.Armorer, tradeskill.TradeskillId);
                Assert.Equal(0u, tradeskill.IsActive);
                Assert.Equal(180u, tradeskill.TradeskillXp);
                Assert.Equal(new uint[] { 7001u }, load.LearnedSchematics);
                Assert.Equal(new uint[] { 1u }, load.LearnedSchematicDiscoveredFlags);

                ServerProfessionsLoad.DiscoveredSchematic discovered = Assert.Single(load.DiscoveredSchematics);
                Assert.Equal(7001u, discovered.TradeskillSchematic2Id);
                Assert.Equal(1.5f, discovered.Coordinates.X);
                Assert.Equal(2.5f, discovered.Coordinates.Y);
            },
            call => Assert.IsType<ServerProfessionModifiers>(call.Arguments[0]));
    }

    [Fact]
    public void EnsureTradeskillTalentPointTotal_BackfillsUnspentRewardPointsAndSendsUpdate()
    {
        GameTableManager gameTableManager = CreateGameTableManager([]);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        var player = CreatePlayer(session, gameTableManager: gameTableManager);

        AddTradeskillState(player, new CharacterTradeskillModel
        {
            Id            = 42ul,
            TradeskillId  = (uint)TradeskillType.Technologist,
            IsActive      = 1u,
            TalentPoints  = 0u,
            TalentTier00  = 111u
        });

        uint added = player.EnsureTradeskillTalentPointTotal(TradeskillType.Technologist, 3u);

        Assert.Equal(2u, added);

        IReadOnlyDictionary<TradeskillType, CharacterTradeskillModel> models = GetTradeskillModels(player);
        Assert.Equal(2u, models[TradeskillType.Technologist].TalentPoints);

        RecordingDispatchProxy<IGameSession>.Invocation call =
            Assert.Single(sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted)));
        var update = Assert.IsType<ServerProfessionUpdate>(call.Arguments[0]);
        Assert.Equal(TradeskillType.Technologist, update.Tradeskill.TradeskillId);
        Assert.Equal(2u, update.Tradeskill.TalentPoints);
        Assert.Equal(111u, update.Tradeskill.TradeskillTalentTierIds[0]);
    }

    [Fact]
    public void ResetTradeskillTalents_ClearsSelectedTalentsWithoutGrantingLegacyFreePoints()
    {
        GameTableManager gameTableManager = CreateGameTableManager([]);
        IGameSession session = RecordingDispatchProxy<IGameSession>.Create(out var sessionProxy);
        var player = CreatePlayer(session, gameTableManager: gameTableManager);

        AddTradeskillState(player, new CharacterTradeskillModel
        {
            Id            = 42ul,
            TradeskillId  = (uint)TradeskillType.Technologist,
            IsActive      = 1u,
            TalentPoints  = 1u,
            TalentTier00  = 111u,
            TalentTier01  = 222u
        });

        bool reset = player.ResetTradeskillTalents(TradeskillType.Technologist);

        Assert.True(reset);

        IReadOnlyDictionary<TradeskillType, CharacterTradeskillModel> models = GetTradeskillModels(player);
        CharacterTradeskillModel technologist = models[TradeskillType.Technologist];
        Assert.Equal(0u, technologist.TalentPoints);
        Assert.Equal(0u, technologist.TalentTier00);
        Assert.Equal(0u, technologist.TalentTier01);

        IReadOnlyList<RecordingDispatchProxy<IGameSession>.Invocation> calls =
            sessionProxy.GetInvocations(nameof(IGameSession.EnqueueMessageEncrypted));
        var update = Assert.IsType<ServerProfessionUpdate>(calls[0].Arguments[0]);
        Assert.Equal(0u, update.Tradeskill.TalentPoints);
        Assert.All(update.Tradeskill.TradeskillTalentTierIds, tier => Assert.Equal(0u, tier));
        Assert.IsType<ServerTradeskillRelearnCooldown>(calls[1].Arguments[0]);
    }

    private static Player CreatePlayer(
        IGameSession session,
        IQuestManager questManager = null,
        ICharacterAchievementManager achievementManager = null,
        IGameTableManager gameTableManager = null)
    {
        var player = (Player)RuntimeHelpers.GetUninitializedObject(typeof(Player));

        SetAutoProperty(player, nameof(Player.Identity), new PlayerIdentity
        {
            Id = 42ul,
            RealmId = 1
        });
        SetAutoProperty(player, nameof(Player.Session), session);
        SetAutoProperty(player, nameof(Player.QuestManager), questManager);
        SetAutoProperty(player, nameof(Player.AchievementManager), achievementManager);
        SetField(player, "gameTableManager", gameTableManager);

        SetField(player, "tradeskills", Activator.CreateInstance(GetFieldInfo(typeof(Player), "tradeskills").FieldType)!);
        SetField(player, "schematics", Activator.CreateInstance(GetFieldInfo(typeof(Player), "schematics").FieldType)!);

        return player;
    }

    private static void AddTradeskillState(Player player, CharacterTradeskillModel model)
    {
        IDictionary tradeskills = (IDictionary)GetFieldInfo(typeof(Player), "tradeskills").GetValue(player)!;
        tradeskills[(TradeskillType)model.TradeskillId] = CreateNestedState("TradeskillState", model);
    }

    private static void AddSchematicState(Player player, CharacterSchematicModel model)
    {
        IDictionary schematics = (IDictionary)GetFieldInfo(typeof(Player), "schematics").GetValue(player)!;
        schematics[model.TradeskillSchematic2Id] = CreateNestedState("SchematicState", model);
    }

    private static IReadOnlyDictionary<TradeskillType, CharacterTradeskillModel> GetTradeskillModels(Player player)
    {
        IDictionary tradeskills = (IDictionary)GetFieldInfo(typeof(Player), "tradeskills").GetValue(player)!;
        return tradeskills.Values
            .Cast<object>()
            .Select(BuildTradeskillModel)
            .ToDictionary(model => (TradeskillType)model.TradeskillId);
    }

    private static CharacterTradeskillModel BuildTradeskillModel(object state)
    {
        return (CharacterTradeskillModel)state.GetType()
            .GetMethod("BuildModel", BindingFlags.Instance | BindingFlags.Public)!
            .Invoke(state, null)!;
    }

    private static object CreateNestedState(string nestedTypeName, object model)
    {
        Type nestedType = typeof(Player).GetNestedType(nestedTypeName, BindingFlags.NonPublic)!;
        return nestedType.GetMethod("FromModel", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [model])!;
    }

    private static GameTableManager CreateGameTableManager(IEnumerable<TradeskillTierEntry> tiers, params TradeskillSchematic2Entry[] schematics)
    {
        var gameTableManager = new GameTableManager(Options.Create(new GameTableConfig
        {
            GameTablePath = string.Empty
        }));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillTier), CreateGameTable(tiers.ToArray()));
        SetAutoProperty(gameTableManager, nameof(GameTableManager.TradeskillSchematic2), CreateGameTable(schematics));

        return gameTableManager;
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

    private static void SetField(object instance, string fieldName, object value)
    {
        GetFieldInfo(instance.GetType(), fieldName).SetValue(instance, value);
    }

    private static FieldInfo GetFieldInfo(Type type, string fieldName)
    {
        return type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;
    }
}
