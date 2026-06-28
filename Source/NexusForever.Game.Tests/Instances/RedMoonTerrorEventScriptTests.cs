using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Raid.RedMoonTerror;

namespace NexusForever.Game.Tests.Instances;

public class RedMoonTerrorEventScriptTests
{
    [Fact]
    public void OnLoad_SetsEnterPhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Enter, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventPhase.Enter, PublicEventObjective.SurviveTheBrig)]
    [InlineData(PublicEventPhase.ChiefWardenLockjaw, PublicEventObjective.DefeatChiefWardenLockjaw)]
    [InlineData(PublicEventPhase.InvestigateTheShredder, PublicEventObjective.InvestigateTheShredder)]
    [InlineData(PublicEventPhase.DefeatSwabbieSkiLi, PublicEventObjective.DefeatSwabbieSkiLi)]
    [InlineData(PublicEventPhase.WasteRelocationShafts, PublicEventObjective.EnterTheWasteRelocationShafts)]
    [InlineData(PublicEventPhase.Robomination, PublicEventObjective.DefeatTheRobomination)]
    [InlineData(PublicEventPhase.EngineeringCompartment, PublicEventObjective.FindAWayToTheEngineeringCompartment)]
    [InlineData(PublicEventPhase.TheEngineers, PublicEventObjective.DefeatTheEngineers)]
    [InlineData(PublicEventPhase.EnterCrewQuarters, PublicEventObjective.MakeYourWayToTheCrewQuarters)]
    [InlineData(PublicEventPhase.MordechaiRedmoon, PublicEventObjective.DefeatMordechaiRedmoon)]
    [InlineData(PublicEventPhase.DestroyAntiBoardingTurrets, PublicEventObjective.DestroyTheAntiBoardingTurret)]
    [InlineData(PublicEventPhase.StarEater, PublicEventObjective.DefeatStarEaterTheVoracious)]
    [InlineData(PublicEventPhase.BreakBackIntoTheRedmoonTerror, PublicEventObjective.BreakBackIntoTheRedmoonTerror)]
    [InlineData(PublicEventPhase.MarauderOfficers, PublicEventObjective.DefeatMarauderOfficers)]
    [InlineData(PublicEventPhase.FindTheNavigationCore, PublicEventObjective.FindTheNavigationCore)]
    [InlineData(PublicEventPhase.Starmap, PublicEventObjective.DefeatTheStarmapSimulation)]
    public void OnPublicEventPhase_SingleObjectivePhases_ActivateMappedObjective(PublicEventPhase phase, PublicEventObjective objective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(objective, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Laveka_SpawnsReviewedStaticPlacement()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out RecordingDispatchProxy<IPublicEvent> eventProxy,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Laveka);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.DefeatLavekaTheDarkHearted, activation.Arguments[0]);

        CreatedNpc npc = Assert.Single(createdNpcs);
        AssertLavekaModel(npc);
        AssertGridEntityAddedToMap(mapProxy, npc.Instance, new Vector3(-723.7178f, 186.8427f, -265.1872f));
    }

    [Fact]
    public void OnPublicEventPhase_Laveka_DoesNotDuplicateReviewedStaticPlacement()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSpawns(
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Laveka);
        script.OnPublicEventPhase((uint)PublicEventPhase.Laveka);

        Assert.Single(createdNpcs);
        Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
    }

    [Fact]
    public void OnPublicEventPhase_EngineeringMinibosses_ActivatesBothObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EngiMinibosses);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatAssistantTechnicianSkooty);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatChiefEngineScrubberThrag);
    }

    [Fact]
    public void OnPublicEventPhase_Medbay_ActivatesBothObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Medbay);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatBonedoctorMuburu);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatHeadshrinkerWgasa);
    }

    [Fact]
    public void OnPublicEventPhase_Morgue_ActivatesBothObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Morgue);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTiny);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatUntombedHorror);
    }

    [Theory]
    [InlineData(PublicEventPhase.Robomination, CommunicatorMessage.IshamelTheBloodied552)]
    [InlineData(PublicEventPhase.EngineeringCompartment, CommunicatorMessage.IshamelTheBloodied224)]
    public void OnPublicEventPhase_BranchMessagePhases_BroadcastMappedIshamelMessage(PublicEventPhase phase, CommunicatorMessage messageId)
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((messageId, message));
        IPublicEvent publicEvent = CreatePublicEvent([player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventObjective.SurviveTheBrig, PublicEventPhase.ChiefWardenLockjaw)]
    [InlineData(PublicEventObjective.DefeatChiefWardenLockjaw, PublicEventPhase.InvestigateTheShredder)]
    [InlineData(PublicEventObjective.InvestigateTheShredder, PublicEventPhase.DefeatSwabbieSkiLi)]
    [InlineData(PublicEventObjective.DefeatSwabbieSkiLi, PublicEventPhase.WasteRelocationShafts)]
    [InlineData(PublicEventObjective.EnterTheWasteRelocationShafts, PublicEventPhase.Robomination)]
    [InlineData(PublicEventObjective.DefeatTheRobomination, PublicEventPhase.EngineeringCompartment)]
    [InlineData(PublicEventObjective.FindAWayToTheEngineeringCompartment, PublicEventPhase.EngiMinibosses)]
    [InlineData(PublicEventObjective.DefeatAssistantTechnicianSkooty, PublicEventPhase.TheEngineers)]
    [InlineData(PublicEventObjective.DefeatChiefEngineScrubberThrag, PublicEventPhase.TheEngineers)]
    [InlineData(PublicEventObjective.DefeatTheEngineers, PublicEventPhase.EnterCrewQuarters)]
    [InlineData(PublicEventObjective.MakeYourWayToTheCrewQuarters, PublicEventPhase.MordechaiRedmoon)]
    [InlineData(PublicEventObjective.DefeatMordechaiRedmoon, PublicEventPhase.DestroyAntiBoardingTurrets)]
    [InlineData(PublicEventObjective.DestroyTheAntiBoardingTurret, PublicEventPhase.StarEater)]
    [InlineData(PublicEventObjective.DefeatStarEaterTheVoracious, PublicEventPhase.BreakBackIntoTheRedmoonTerror)]
    [InlineData(PublicEventObjective.BreakBackIntoTheRedmoonTerror, PublicEventPhase.MarauderOfficers)]
    [InlineData(PublicEventObjective.DefeatMarauderOfficers, PublicEventPhase.FindTheNavigationCore)]
    [InlineData(PublicEventObjective.FindTheNavigationCore, PublicEventPhase.Starmap)]
    [InlineData(PublicEventObjective.DefeatTheStarmapSimulation, PublicEventPhase.Medbay)]
    [InlineData(PublicEventObjective.DefeatBonedoctorMuburu, PublicEventPhase.Morgue)]
    [InlineData(PublicEventObjective.DefeatHeadshrinkerWgasa, PublicEventPhase.Morgue)]
    [InlineData(PublicEventObjective.DefeatTiny, PublicEventPhase.Laveka)]
    [InlineData(PublicEventObjective.DefeatUntombedHorror, PublicEventPhase.Laveka)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatLavekaTheDarkHearted, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SurviveTheBrig, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static IPublicEvent CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent([], out eventProxy);
    }

    private static IPublicEvent CreatePublicEvent(IReadOnlyList<IPlayer> players, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithReviewedSpawns(
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<CreatedNpc> createdNpcs)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3032u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), Array.Empty<IPlayer>());

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<CreatedNpc> npcs = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            CreatedNpc npc = CreateNpc();
            npcs.Add(npc);
            return npc.Instance;
        });

        createdNpcs = npcs;
        return publicEvent;
    }

    private static IPublicEventObjective CreateObjective(PublicEventObjective objective, PublicEventStatus status)
    {
        IPublicEventObjective eventObjective = RecordingDispatchProxy<IPublicEventObjective>.Create(out RecordingDispatchProxy<IPublicEventObjective> objectiveProxy);
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry
        {
            Id = (uint)objective
        });
        objectiveProxy.SetProperty(nameof(IPublicEventObjective.Status), status);
        return eventObjective;
    }

    private static RedMoonTerrorEventScript CreateScript(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new RedMoonTerrorEventScript(globalQuestManager);
    }

    private static IPlayer CreatePlayer(out IGameSession session)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        return player;
    }

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
    }

    private static void AssertLavekaModel(CreatedNpc npc)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300056u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(65997u, model.Creature);
        Assert.Equal((ushort)3032u, model.World);
        Assert.Equal((ushort)5996u, model.Area);
        Assert.Equal(-723.7178f, model.X);
        Assert.Equal(186.8427f, model.Y);
        Assert.Equal(-265.1872f, model.Z);
        Assert.Equal(MathF.PI, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(38426u, model.DisplayInfo);
        Assert.Equal((ushort)0u, model.OutfitInfo);
        Assert.Equal((ushort)1351u, model.Faction1);
        Assert.Equal((ushort)1351u, model.Faction2);
        Assert.Null(model.EntityEvent);
        Assert.Collection(model.EntityScript,
            entityScript => Assert.Equal("LavekaTheDarkHeartedEntityScript", entityScript.ScriptName));
        Assert.Collection(model.EntityStat.OrderBy(s => s.Stat),
            health =>
            {
                Assert.Equal((byte)Stat.Health, health.Stat);
                Assert.Equal(1f, health.Value);
            },
            level =>
            {
                Assert.Equal((byte)Stat.Level, level.Stat);
                Assert.Equal(50f, level.Value);
            });
    }

    private static void AssertGridEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity entity,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)),
            i => ReferenceEquals(entity, i.Arguments[0]));
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3032u, position.Info.Entry.Id);
    }

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);
}
