using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Options;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Combat;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Prerequisite;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Network.World.Message.Model;
using NexusForever.Network.World.Message.Static;
using NexusForever.WorldServer.Network;
using NexusForever.WorldServer.Network.Message.Handler.Entity;

namespace NexusForever.Game.Tests.Entity;

public class ClientActivateUnitCastHandlerTests
{
    [Fact]
    public void HandleMessageInternal_WithAttackableUnit_TargetsAndStartsThreatWithoutCastingActivateSpell()
    {
        ClientActivateUnitCastHandler handler = CreateHandler();
        IWorldSession session = CreateAttackableUnitSession(
            out IUnitEntity target,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IUnitEntity> targetProxy,
            out RecordingDispatchProxy<IThreatManager> threatProxy);

        InvokeHandleMessageInternal(handler, session, 77u, 0u, nameof(ClientActivateUnitCast));

        Assert.Contains(playerProxy.GetInvocations(nameof(IPlayer.SetTarget)), i =>
            i.Arguments.Length == 2
            && ReferenceEquals(i.Arguments[0], target)
            && (uint)i.Arguments[1] == 1u);
        Assert.Contains(threatProxy.GetInvocations(nameof(IThreatManager.UpdateThreat)), i =>
            i.Arguments.Length == 2
            && ReferenceEquals(i.Arguments[0], session.Player)
            && (int)i.Arguments[1] == 1);
        Assert.Empty(targetProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(targetProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
    }

    [Fact]
    public void HandleMessageInternal_WithTutorialMineAndBlockedActivateSpell_CompletesActivation()
    {
        ClientActivateUnitCastHandler handler = CreateHandler();
        IWorldSession session = CreateSession(
            creatureId: 73463u,
            castResult: CastResult.TargetUnknown,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IWorldEntity> entityProxy);

        InvokeHandleMessageInternal(handler, session, 77u, 0u, nameof(ClientActivateUnitCast));

        Assert.Single(playerProxy.GetInvocations("TryCastSpell"));
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
    }

    [Fact]
    public void HandleMessageInternal_WithNonMineAndBlockedActivateSpell_FailsActivation()
    {
        ClientActivateUnitCastHandler handler = CreateHandler();
        IWorldSession session = CreateSession(
            creatureId: 70000u,
            castResult: CastResult.TargetUnknown,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IWorldEntity> entityProxy);

        InvokeHandleMessageInternal(handler, session, 77u, 0u, nameof(ClientActivateUnitCast));

        Assert.Single(playerProxy.GetInvocations("TryCastSpell"));
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
    }

    [Fact]
    public void HandleMessageInternal_WithActivateSpellPrerequisite_EvaluatesPrerequisiteAgainstActivatedUnit()
    {
        IPrerequisiteManager prerequisiteManager = RecordingDispatchProxy<IPrerequisiteManager>.Create(out RecordingDispatchProxy<IPrerequisiteManager> prerequisiteProxy);
        ClientActivateUnitCastHandler handler = CreateHandler(prerequisiteManager);
        IWorldSession session = CreateUnitSession(
            creatureId: 21793u,
            castResult: CastResult.Ok,
            activateSpellId: 1817u,
            activatePrerequisiteId: 1050u,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IUnitEntity> entityProxy,
            out IUnitEntity entity);

        prerequisiteProxy.SetMethodHandler(nameof(IPrerequisiteManager.Meets), args =>
        {
            Assert.Equal(3, args.Length);
            Assert.Same(session.Player, args[0]);
            Assert.Equal(1050u, (uint)args[1]);

            IPrerequisiteParameters parameters = Assert.IsAssignableFrom<IPrerequisiteParameters>(args[2]);
            Assert.Same(entity, parameters.Target);
            return true;
        });

        InvokeHandleMessageInternal(handler, session, 77u, 19u, nameof(ClientActivateUnitCast));

        RecordingDispatchProxy<IPlayer>.Invocation cast = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(1817u, (uint)cast.Arguments[0]);
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateCast)));
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
    }

    [Fact]
    public void HandleMessageInternal_WithTutorialHoverboardProjectorAndBlockedActivateSpell_CastsDirectMountAndCompletesActivation()
    {
        ClientActivateUnitCastHandler handler = CreateHandler();
        IWorldSession session = CreateSession(
            creatureId: 73419u,
            castResult: CastResult.TargetUnknown,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IWorldEntity> entityProxy,
            mountCastResult: CastResult.Ok,
            activateSpellId: 86744u);

        InvokeHandleMessageInternal(handler, session, 77u, 0u, nameof(ClientActivateUnitCast));

        uint[] spellIds = playerProxy
            .GetInvocations(nameof(IPlayer.TryCastSpell))
            .Select(i => (uint)i.Arguments[0])
            .ToArray();

        Assert.Equal([86744u, 85562u], spellIds);
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
    }

    [Fact]
    public void HandleMessageInternal_WithTutorialHoverboardProjectorAndBlockedMountCast_FailsActivation()
    {
        ClientActivateUnitCastHandler handler = CreateHandler();
        IWorldSession session = CreateSession(
            creatureId: 73419u,
            castResult: CastResult.TargetUnknown,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IWorldEntity> entityProxy,
            mountCastResult: CastResult.TargetUnknown,
            activateSpellId: 86744u);

        InvokeHandleMessageInternal(handler, session, 77u, 0u, nameof(ClientActivateUnitCast));

        uint[] spellIds = playerProxy
            .GetInvocations(nameof(IPlayer.TryCastSpell))
            .Select(i => (uint)i.Arguments[0])
            .ToArray();

        Assert.Equal([86744u, 85562u], spellIds);
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
    }

    [Fact]
    public void HandleMessageInternal_WithTutorialHoverboardProjectorAndMissingActivateSpell_CastsDirectMountAndCompletesActivation()
    {
        ClientActivateUnitCastHandler handler = CreateHandler();
        IWorldSession session = CreateSession(
            creatureId: 73419u,
            castResult: CastResult.NoValidActivateSpell,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IWorldEntity> entityProxy,
            mountCastResult: CastResult.Ok,
            activateSpellId: 0u);

        InvokeHandleMessageInternal(handler, session, 77u, 0u, nameof(ClientActivateUnitCast));

        RecordingDispatchProxy<IPlayer>.Invocation mountCast = Assert.Single(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Equal(85562u, (uint)mountCast.Arguments[0]);
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
    }

    [Fact]
    public void HandleMessageInternal_WithTutorialHousingProjectorAndMissingActivateSpell_CompletesActivationWithoutCasting()
    {
        ClientActivateUnitCastHandler handler = CreateHandler();
        IWorldSession session = CreateSession(
            creatureId: 73741u,
            castResult: CastResult.NoValidActivateSpell,
            out RecordingDispatchProxy<IPlayer> playerProxy,
            out RecordingDispatchProxy<IWorldEntity> entityProxy,
            activateSpellId: 0u);

        InvokeHandleMessageInternal(handler, session, 77u, 0u, nameof(ClientActivateUnitCast));

        Assert.Empty(playerProxy.GetInvocations(nameof(IPlayer.TryCastSpell)));
        Assert.Single(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateSuccess)));
        Assert.Empty(entityProxy.GetInvocations(nameof(IWorldEntity.OnActivateFail)));
    }

    private static ClientActivateUnitCastHandler CreateHandler(IPrerequisiteManager prerequisiteManager = null)
    {
        prerequisiteManager ??= RecordingDispatchProxy<IPrerequisiteManager>.Create(out _);
        IAssetManager assetManager = RecordingDispatchProxy<IAssetManager>.Create(out _);
        return new ClientActivateUnitCastHandler(prerequisiteManager, assetManager, CreateGameTableManager());
    }

    private static IWorldSession CreateSession(
        uint creatureId,
        CastResult castResult,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IWorldEntity> entityProxy,
        CastResult? mountCastResult = null,
        uint activateSpellId = 85452u)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3460u });

        questProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), Array.Empty<IQuest>());

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetMethodHandler(nameof(IWorldSession.TryConsumeNextClientSpellEvidenceCapture), args =>
        {
            args[0] = false;
            return false;
        });

        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 17u);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetMethodReturn("GetVisible", entity);
        playerProxy.SetMethodHandler(nameof(IPlayer.TryCastSpell), args =>
        {
            uint spell4Id = (uint)args[0];
            return spell4Id == 85562u ? mountCastResult ?? castResult : castResult;
        });

        entityProxy.SetProperty(nameof(IGridEntity.Guid), 77u);
        entityProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);
        entityProxy.SetProperty(nameof(IWorldEntity.IsBusy), false);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry
        {
            Id = creatureId,
            Spell4IdActivate00 = activateSpellId,
            ActivateSpellMaxRange = 0f
        });

        return session;
    }

    private static IWorldSession CreateUnitSession(
        uint creatureId,
        CastResult castResult,
        uint activateSpellId,
        uint activatePrerequisiteId,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IUnitEntity> entityProxy,
        out IUnitEntity entity)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        entity = RecordingDispatchProxy<IUnitEntity>.Create(out entityProxy);
        IQuestManager questManager = RecordingDispatchProxy<IQuestManager>.Create(out RecordingDispatchProxy<IQuestManager> questProxy);
        ICharacterAchievementManager achievementManager = RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 51u });

        questProxy.SetMethodReturn(nameof(IQuestManager.GetActiveQuests), Array.Empty<IQuest>());

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);
        sessionProxy.SetMethodHandler(nameof(IWorldSession.TryConsumeNextClientSpellEvidenceCapture), args =>
        {
            args[0] = false;
            return false;
        });

        playerProxy.SetProperty(nameof(IPlayer.Map), map);
        playerProxy.SetProperty(nameof(IPlayer.Guid), 17u);
        playerProxy.SetProperty(nameof(IPlayer.Position), Vector3.Zero);
        playerProxy.SetProperty(nameof(IPlayer.QuestManager), questManager);
        playerProxy.SetProperty(nameof(IPlayer.AchievementManager), achievementManager);
        playerProxy.SetMethodReturn("GetVisible", entity);
        playerProxy.SetMethodHandler(nameof(IPlayer.TryCastSpell), _ => castResult);

        entityProxy.SetProperty(nameof(IGridEntity.Guid), 77u);
        entityProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);
        entityProxy.SetProperty(nameof(IWorldEntity.IsBusy), false);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), creatureId);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureEntry), new Creature2Entry
        {
            Id = creatureId,
            Spell4IdActivate00 = activateSpellId,
            PrerequisiteIdActivateSpell00 = activatePrerequisiteId,
            ActivateSpellMaxRange = 0f
        });

        return session;
    }

    private static IWorldSession CreateAttackableUnitSession(
        out IUnitEntity target,
        out RecordingDispatchProxy<IPlayer> playerProxy,
        out RecordingDispatchProxy<IUnitEntity> targetProxy,
        out RecordingDispatchProxy<IThreatManager> threatProxy)
    {
        IWorldSession session = RecordingDispatchProxy<IWorldSession>.Create(out RecordingDispatchProxy<IWorldSession> sessionProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out playerProxy);
        target = RecordingDispatchProxy<IUnitEntity>.Create(out targetProxy);
        IThreatManager threatManager = RecordingDispatchProxy<IThreatManager>.Create(out threatProxy);

        sessionProxy.SetProperty(nameof(IWorldSession.Player), player);

        playerProxy.SetProperty(nameof(IPlayer.Guid), 17u);
        playerProxy.SetMethodReturn("GetVisible", target);
        playerProxy.SetMethodReturn(nameof(IPlayer.CanAttack), true);

        targetProxy.SetProperty(nameof(IGridEntity.Guid), 77u);
        targetProxy.SetProperty(nameof(IGridEntity.Position), Vector3.Zero);
        targetProxy.SetProperty(nameof(IWorldEntity.CreatureId), 70000u);
        targetProxy.SetProperty(nameof(IWorldEntity.IsBusy), false);
        targetProxy.SetProperty(nameof(IUnitEntity.ThreatManager), threatManager);

        return session;
    }

    private static GameTableManager CreateGameTableManager()
    {
        GameTableManager gameTableManager = new(Options.Create(new GameTableConfig()));
        SetProperty(gameTableManager, nameof(GameTableManager.Creature2), CreateCreatureTable(
            CreateCreatureEntry(73419u),
            CreateCreatureEntry(73463u),
            CreateCreatureEntry(73667u),
            CreateCreatureEntry(73668u),
            CreateCreatureEntry(73741u)));

        return gameTableManager;
    }

    private static GameTable<Creature2Entry> CreateCreatureTable(params Creature2Entry[] entries)
    {
        GameTable<Creature2Entry> gameTable = (GameTable<Creature2Entry>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<Creature2Entry>));
        uint maxId = 0u;
        foreach (Creature2Entry entry in entries)
            maxId = Math.Max(maxId, entry.Id);

        int[] lookup = new int[checked((int)maxId + 1)];
        Array.Fill(lookup, -1);

        for (int index = 0; index < entries.Length; index++)
            lookup[entries[index].Id] = index;

        SetProperty(gameTable, nameof(GameTable<Creature2Entry>.Entries), entries);
        SetField(gameTable, "lookup", lookup);
        SetField(gameTable, "header", new GameTableHeader
        {
            MaxId = (ulong)lookup.Length
        });

        return gameTable;
    }

    private static Creature2Entry CreateCreatureEntry(uint creatureId)
    {
        return new Creature2Entry
        {
            Id = creatureId,
            Spell4IdActivate00 = 85452u,
            ActivateSpellMaxRange = 0f
        };
    }

    private static void SetProperty(object instance, string propertyName, object value)
    {
        PropertyInfo property = instance.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        Assert.NotNull(property);
        property.SetValue(instance, value);
    }

    private static void SetField(object instance, string fieldName, object value)
    {
        FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(instance, value);
    }

    private static void InvokeHandleMessageInternal(ClientActivateUnitCastHandler handler, IWorldSession session, uint activateUnitId, uint contextToken, string clientRequestSource)
    {
        MethodInfo method = typeof(ClientActivateUnitCastHandler).GetMethod(
            "HandleMessageInternal",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        method.Invoke(handler, [session, activateUnitId, contextToken, clientRequestSource]);
    }
}
