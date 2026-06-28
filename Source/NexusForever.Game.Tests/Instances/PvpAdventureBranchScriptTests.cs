using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Matching;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Adventure.WarOfTheWilds.Script;
using CryoCreature = NexusForever.Script.Instance.Arena.TheCryoPlex.PublicEventCreature;
using CryoEventScript = NexusForever.Script.Instance.Arena.TheCryoPlex.TheCryoPlexEventScript;
using CryoObjective = NexusForever.Script.Instance.Arena.TheCryoPlex.PublicEventObjective;
using CryoPhase = NexusForever.Script.Instance.Arena.TheCryoPlex.PublicEventPhase;
using CryoSubEventScript = NexusForever.Script.Instance.Arena.TheCryoPlex.TheCryoPlexSubEventScript;
using WarEventScript = NexusForever.Script.Instance.Adventure.WarOfTheWilds.WarOfTheWildsAdventureEventScript;
using WarObjective = NexusForever.Script.Instance.Adventure.WarOfTheWilds.PublicEventObjective;
using WarPhase = NexusForever.Script.Instance.Adventure.WarOfTheWilds.PublicEventPhase;

namespace NexusForever.Game.Tests.Instances;

public class PvpAdventureBranchScriptTests
{
    [Fact]
    public void WarOfTheWilds_OnLoad_SetsBranchFightPhase()
    {
        _ = CreateWarScript(out var publicEventProxy, preserveLoadInvocations: true);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(WarPhase.Fight, invocation.Arguments[0]);
    }

    [Fact]
    public void WarOfTheWilds_Fight_ActivatesMappedTotemObjectives()
    {
        WarEventScript script = CreateWarScript(out var publicEventProxy);

        script.OnPublicEventPhase((uint)WarPhase.Fight);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = publicEventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (WarObjective)i.Arguments[0] == WarObjective.DestroyTheGiantMoodieTotem);
        Assert.Contains(activations, i => (WarObjective)i.Arguments[0] == WarObjective.MoodieTotemHealth);
        Assert.Contains(activations, i => (WarObjective)i.Arguments[0] == WarObjective.SkeechTotemHealth);
    }

    [Fact]
    public void WarOfTheWilds_DestroyTotemSuccess_FinishesPublicEvent()
    {
        WarEventScript script = CreateWarScript(out var publicEventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(WarObjective.DestroyTheGiantMoodieTotem, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void WarOfTheWilds_GiantMoodieTotemDeath_CreditsDestroyAndHealthResourcePoolsToFullCount()
    {
        GiantMoodieTotemEntityScript script = CreateGiantMoodieTotemScript(out var publicEventManagerProxy);

        script.OnDeath();

        List<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates = publicEventManagerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .Where(invocation => invocation.Arguments.Length == 2)
            .ToList();
        Assert.Contains(updates, invocation =>
            (WarObjective)invocation.Arguments[0] == WarObjective.DestroyTheGiantMoodieTotem &&
            (int)invocation.Arguments[1] == 100);
        Assert.Contains(updates, invocation =>
            (WarObjective)invocation.Arguments[0] == WarObjective.MoodieTotemHealth &&
            (int)invocation.Arguments[1] == 100);
    }

    [Fact]
    public void WarOfTheWilds_GiantMoodieTotemDeath_WhenRepeated_CreditsOnce()
    {
        GiantMoodieTotemEntityScript script = CreateGiantMoodieTotemScript(out var publicEventManagerProxy);

        script.OnDeath();
        script.OnDeath();

        Assert.Equal(2, publicEventManagerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .Count(invocation => invocation.Arguments.Length == 2));
    }

    [Fact]
    public void CryoPlexEvent_OnMatchState_SetsBranchPhases()
    {
        CryoEventScript script = CreateCryoScript(out var publicEventProxy);

        script.OnMatchState(PvpGameState.InProgress);
        script.OnMatchState(PvpGameState.Finished);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> phaseChanges = publicEventProxy
            .GetInvocations(nameof(IPublicEvent.SetPhase))
            .ToList();
        Assert.Contains(phaseChanges, i => (CryoPhase)i.Arguments[0] == CryoPhase.Fight);
        Assert.Contains(phaseChanges, i => (CryoPhase)i.Arguments[0] == CryoPhase.Finished);
    }

    [Fact]
    public void CryoPlexEvent_Fight_OpensTrackedForcefieldDoor()
    {
        CryoEventScript script = CreateCryoScript(out var publicEventProxy, out var mapProxy);
        IWorldEntity forcefield = CreateWorldEntity(CryoCreature.PvpForcefieldDoor, 901u);
        IDoorEntity door = RecordingDispatchProxy<IDoorEntity>.Create(out var doorProxy);

        script.OnAddToMap(forcefield);
        mapProxy.SetMethodReturn(nameof(IBaseMap.GetEntity), door);

        script.OnPublicEventPhase((uint)CryoPhase.Fight);

        Assert.Single(doorProxy.GetInvocations(nameof(IDoorEntity.OpenDoor)));
    }

    [Fact]
    public void CryoPlexEvent_Death_UpdatesStatsAndAutoResurrects()
    {
        CryoEventScript script = CreateCryoScript(out var publicEventProxy, out _, out var playerManagerProxy);
        IPlayer player = CreatePlayer(77ul, isAlive: false, out var resurrectionProxy);

        playerManagerProxy.SetMethodReturn(nameof(IPlayerManager.GetPlayer), player);

        script.OnDeath(player);
        script.Update(5d);

        RecordingDispatchProxy<IPublicEvent>.Invocation stat = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateStat)));
        Assert.Same(player, stat.Arguments[0]);
        Assert.Equal(PublicEventStat.Deaths, stat.Arguments[1]);
        Assert.Equal(1u, stat.Arguments[2]);

        RecordingDispatchProxy<IResurrectionManager>.Invocation resurrect = Assert.Single(
            resurrectionProxy.GetInvocations(nameof(IResurrectionManager.Resurrect)));
        Assert.Equal(ResurrectionType.Holocrypt, resurrect.Arguments[0]);
    }

    [Fact]
    public void CryoPlexSubEvent_PrepareSuccess_AdvancesToFight()
    {
        CryoSubEventScript script = CreateCryoSubEventScript(out var publicEventProxy);

        script.OnPublicEventObjectiveStatus(CreateObjective(CryoObjective.PrepareForBattle, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation phase = Assert.Single(publicEventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(CryoPhase.Fight, phase.Arguments[0]);
    }

    [Fact]
    public void CryoPlexSubEvent_Fight_ActivatesParticipateObjective()
    {
        CryoSubEventScript script = CreateCryoSubEventScript(out var publicEventProxy);

        script.OnPublicEventPhase((uint)CryoPhase.Fight);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            publicEventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(CryoObjective.ParticipateInArena, activation.Arguments[0]);
    }

    [Fact]
    public void CryoPlexSubEvent_MatchStart_CompletesPreparationObjective()
    {
        CryoSubEventScript script = CreateCryoSubEventScript(out var publicEventProxy);

        script.OnMatchState(PvpGameState.InProgress);

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(
            publicEventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(CryoObjective.PrepareForBattle, update.Arguments[0]);
        Assert.Equal(0, update.Arguments[1]);
    }

    private static WarEventScript CreateWarScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        bool preserveLoadInvocations = false)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out publicEventProxy);
        var script = new WarEventScript();
        script.OnLoad(publicEvent);

        if (!preserveLoadInvocations)
            publicEventProxy.Invocations.Clear();

        return script;
    }

    private static GiantMoodieTotemEntityScript CreateGiantMoodieTotemScript(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out var mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        ICreatureEntity creature = RecordingDispatchProxy<ICreatureEntity>.Create(out var creatureProxy);
        creatureProxy.SetProperty(nameof(IGridEntity.Map), map);
        creatureProxy.SetProperty(nameof(IWorldEntity.CreatureId), 25952u);

        var script = new GiantMoodieTotemEntityScript();
        script.OnLoad(creature);
        return script;
    }

    private static CryoEventScript CreateCryoScript(out RecordingDispatchProxy<IPublicEvent> publicEventProxy)
    {
        return CreateCryoScript(out publicEventProxy, out _, out _);
    }

    private static CryoEventScript CreateCryoScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IBaseMap> mapProxy)
    {
        return CreateCryoScript(out publicEventProxy, out mapProxy, out _);
    }

    private static CryoEventScript CreateCryoScript(
        out RecordingDispatchProxy<IPublicEvent> publicEventProxy,
        out RecordingDispatchProxy<IBaseMap> mapProxy,
        out RecordingDispatchProxy<IPlayerManager> playerManagerProxy)
    {
        IPlayerManager playerManager = RecordingDispatchProxy<IPlayerManager>.Create(out playerManagerProxy);
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out publicEventProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out mapProxy);
        publicEventProxy.SetProperty(nameof(IPublicEvent.Map), map);

        var script = new CryoEventScript(RecordingDispatchProxy<NexusForever.Game.Abstract.Matching.IMatchingDataManager>.Create(out _), playerManager);
        script.OnLoad(publicEvent);
        publicEventProxy.Invocations.Clear();
        return script;
    }

    private static CryoSubEventScript CreateCryoSubEventScript(out RecordingDispatchProxy<IPublicEvent> publicEventProxy)
    {
        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out publicEventProxy);
        var script = new CryoSubEventScript();
        script.OnLoad(publicEvent);
        publicEventProxy.Invocations.Clear();
        return script;
    }

    private static IWorldEntity CreateWorldEntity(CryoCreature creature, uint guid)
    {
        IWorldEntity entity = RecordingDispatchProxy<IWorldEntity>.Create(out var entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Guid), guid);
        entityProxy.SetProperty(nameof(IWorldEntity.CreatureId), (uint)creature);
        return entity;
    }

    private static IPlayer CreatePlayer(
        ulong characterId,
        bool isAlive,
        out RecordingDispatchProxy<IResurrectionManager> resurrectionProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        IResurrectionManager resurrectionManager = RecordingDispatchProxy<IResurrectionManager>.Create(out resurrectionProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), characterId);
        playerProxy.SetProperty(nameof(IUnitEntity.IsAlive), isAlive);
        playerProxy.SetProperty(nameof(IPlayer.ResurrectionManager), resurrectionManager);
        return player;
    }

    private static IPublicEventObjective CreateObjective<T>(T objective, PublicEventStatus status) where T : Enum
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = Convert.ToUInt32(objective)
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        return eventObjective;
    }
}
