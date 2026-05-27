using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Raid.GeneticArchives;

namespace NexusForever.Game.Tests.Instances;

public class GeneticArchivesEventScriptTests
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
    [InlineData(PublicEventPhase.GetToNextLevel, PublicEventObjective.DefeatTheFetidMiscreation)]
    [InlineData(PublicEventPhase.PhagebornConvergence, PublicEventObjective.DefeatThePhagebornConvergence)]
    [InlineData(PublicEventPhase.Ohmna, PublicEventObjective.DefeatDreadphageOhmna)]
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
    public void OnPublicEventPhase_SecondFloor_ActivatesBothGuardianObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SecondFloor);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatPhagetechGuardianC148);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatPhagetechGuardianC432);
    }

    [Fact]
    public void OnPublicEventPhase_ArchiveDefenseSystem_ActivatesObjectiveAndChallenge()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ArchiveDefenseSystem);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.GetThroughTheArchiveDefenseSystem);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DeathFromAbove);
    }

    [Fact]
    public void OnPublicEventPhase_Minibosses_ActivatesAllFourObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Minibosses);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheMalfunctioningPiston);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheMalfunctioningBattery);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheMalfunctioningGear);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheMalfunctioningDynamo);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_BroadcastsMappedOhmnaMessage()
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((CommunicatorMessage.TheDreadphageOhmna1, message));
        IPublicEvent publicEvent = CreatePublicEvent([player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_Ohmna_QueuesWipOpenDoorCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(CreateCinematicFactory(cinematic));
        IPublicEvent publicEvent = CreatePublicEvent([player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Ohmna);

        RecordingDispatchProxy<ICinematicManager>.Invocation queue = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queue.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventObjective.DefeatExperimentX89, PublicEventPhase.GetToNextLevel)]
    [InlineData(PublicEventObjective.DefeatKuralakTheDefiler, PublicEventPhase.GetToNextLevel)]
    [InlineData(PublicEventObjective.DefeatTheFetidMiscreation, PublicEventPhase.SecondFloor)]
    [InlineData(PublicEventObjective.DefeatPhageMaw, PublicEventPhase.ArchiveDefenseSystem)]
    [InlineData(PublicEventObjective.DefeatThePhagetechPrototypes, PublicEventPhase.ArchiveDefenseSystem)]
    [InlineData(PublicEventObjective.GetThroughTheArchiveDefenseSystem, PublicEventPhase.PhagebornConvergence)]
    [InlineData(PublicEventObjective.DefeatThePhagebornConvergence, PublicEventPhase.Minibosses)]
    [InlineData(PublicEventObjective.DefeatTheMalfunctioningGear, PublicEventPhase.Ohmna)]
    [InlineData(PublicEventObjective.DefeatTheMalfunctioningPiston, PublicEventPhase.Ohmna)]
    [InlineData(PublicEventObjective.DefeatTheMalfunctioningDynamo, PublicEventPhase.Ohmna)]
    [InlineData(PublicEventObjective.DefeatTheMalfunctioningBattery, PublicEventPhase.Ohmna)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Theory]
    [InlineData(PublicEventObjective.DefeatPhagetechGuardianC148, PublicEventObjective.DefeatConstructsInTheCentrifuge)]
    [InlineData(PublicEventObjective.DefeatConstructsInTheCentrifuge, PublicEventObjective.DefeatPhageMaw)]
    [InlineData(PublicEventObjective.DefeatPhagetechGuardianC432, PublicEventObjective.DefeatTheParagonsOfSymbiosis)]
    [InlineData(PublicEventObjective.DefeatTheParagonsOfSymbiosis, PublicEventObjective.DefeatThePhagetechPrototypes)]
    public void OnPublicEventObjectiveStatus_Success_ActivatesBranchSubObjective(PublicEventObjective objective, PublicEventObjective nextObjective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(nextObjective, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatDreadphageOhmna, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatExperimentX89, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Theory]
    [InlineData(PublicEventPhase.Enter, CommunicatorMessage.TheDreadphageOhmna1)]
    [InlineData(PublicEventPhase.Ohmna, CommunicatorMessage.TheDreadphageOhmna8)]
    public void OnCinematicFinish_BranchMessagePhases_SendMappedOhmnaMessage(PublicEventPhase phase, CommunicatorMessage messageId)
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((messageId, message));
        IPublicEvent publicEvent = CreatePublicEvent([player], out RecordingDispatchProxy<IPublicEvent> eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Phase), (uint)phase);
        script.OnLoad(publicEvent);

        script.OnCinematicFinish(player, 0u);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
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

    private static GeneticArchivesEventScript CreateScript(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _), messages);
    }

    private static GeneticArchivesEventScript CreateScript(
        ICinematicFactory cinematicFactory,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new GeneticArchivesEventScript(globalQuestManager, cinematicFactory);
    }

    private static IPlayer CreatePlayer(out IGameSession session)
    {
        return CreatePlayer(out session, out _);
    }

    private static IPlayer CreatePlayer(out IGameSession session, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        session = RecordingDispatchProxy<IGameSession>.Create(out _);
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);
        playerProxy.SetProperty(nameof(IPlayer.Session), session);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        return player;
    }

    private static ICinematicFactory CreateCinematicFactory(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }
}
