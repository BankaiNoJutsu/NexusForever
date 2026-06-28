using System.Numerics;
using NexusForever.Database.World.Model;
using NexusForever.Game.Abstract;
using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Entity.Trigger;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Shared;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script.Instance.Expedition.FragmentZero;
using NexusForever.Script.Instance.Expedition.FragmentZero.Script;
using NexusForever.Script.Template.Filter;

namespace NexusForever.Game.Tests.Instances;

public class FragmentZeroEventScriptTests
{
    [Fact]
    public void OnLoad_StartsAtBranchEnabledSearchContinuation()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.ContinueTheSearchForTheMissingCrew, invocation.Arguments[0]);
    }

    [Fact]
    public void OnLoad_ActivatesGoldMedalTimerObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.GoldMedalTimer, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_SearchContinuation_ActivatesBranchObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSupervisorLola(5u, out RecordingDispatchProxy<IPublicEvent> eventProxy, out _, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ContinueTheSearchForTheMissingCrew
            && (uint)i.Arguments[1] == 5u);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.LocateTheShipsBlackBox);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EliminateSkeech);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea2);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea3);
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DiscoverTheTwoLostRecordings
            && (uint)i.Arguments[1] == 2u);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.FindOhmnasHiddenRecording);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.FindLucentsHiddenRecording);
    }

    [Fact]
    public void OnPublicEventPhase_SearchForMissingCrew_CreatesWipGuessedTurnstileTrigger()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            5u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<RecordingTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SearchForMissingCrew);

        RecordingTrigger trigger = Assert.Single(createdTriggers);
        Assert.Equal((4397u, 15f, 4397u), Assert.Single(trigger.TurnstileInitialiseCalls));
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(9865.48f, -765.58f, -5896.74f));
    }

    [Theory]
    [InlineData(PublicEventPhase.FollowTheFriendlySkeech, 49225u, 7902u, 9894.729f, -783.358f, -6032.538f)]
    [InlineData(PublicEventPhase.ReturnToTheAirlockOfTheLifeOverseersCreche, 48682u, 7893u, 9830.69f, -780.754f, -6315.97f)]
    public void OnPublicEventPhase_BranchWorldLocationPhases_CreateWipGuessedTrigger(
        PublicEventPhase phase,
        uint worldLocationId,
        uint objectId,
        float x,
        float y,
        float z)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            5u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<RecordingTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingTrigger trigger = Assert.Single(createdTriggers);
        Assert.Equal((worldLocationId, objectId), Assert.Single(trigger.WorldLocationInitialiseCalls));
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(x, y, z));
    }

    [Fact]
    public void OnPublicEventPhase_SearchContinuation_SpawnsReviewedSupervisorLolaAndWipGuessedTrigger()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSupervisorLola(
            5u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<RecordingTrigger> createdTriggers,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew);

        CreatedNpc supervisorLola = Assert.Single(createdNpcs);
        AssertSupervisorLolaModel(supervisorLola);
        AssertEntityAddedToMap(mapProxy, supervisorLola.Instance, new Vector3(9873.75f, -765.9531f, -5848.58f), 0);

        RecordingTrigger trigger = Assert.Single(createdTriggers);
        Assert.Equal((48711u, 7903u), Assert.Single(trigger.WorldLocationInitialiseCalls));
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(9685.685f, -767.6072f, -6098.117f), 1);
    }

    [Fact]
    public void OnPublicEventPhase_SearchContinuation_DoesNotDuplicateReviewedSupervisorLolaSpawn()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSupervisorLola(
            5u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out _,
            out List<CreatedNpc> createdNpcs);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew);
        script.OnPublicEventPhase((uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew);

        Assert.Single(createdNpcs);
        Assert.Equal(3, mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)).Count());
    }

    [Fact]
    public void OnPublicEventPhase_SearchContinuation_QueuesWipFirstWarningCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        ICinematicFactory cinematicFactory = CreateCinematicFactory(cinematic);
        IPlayer player = CreatePlayer(out _, out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(cinematicFactory);
        IPublicEvent publicEvent = CreatePublicEventWithReviewedSupervisorLola(1u, [player], out _, out _, out _, out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew);

        RecordingDispatchProxy<ICinematicManager>.Invocation queue = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queue.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatPrototypes_ActivatesAllThreePrototypeObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatPrototypes);

        List<object> objectives = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .Select(i => i.Arguments[0])
            .ToList();
        Assert.Contains(PublicEventObjective.DefeatPrototypeAlphansideTheIncubationComplex, objectives);
        Assert.Contains(PublicEventObjective.DefeatPrototypeBeta, objectives);
        Assert.Contains(PublicEventObjective.DefeatPrototypeDelta, objectives);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatProjectMatron_ActivatesMappedObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatProjectMatron);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatProjectMatron);
        Assert.Equal(PublicEventObjective.DefeatProjectMatron, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_DefeatTheLifeOverseer_ActivatesMappedObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatTheLifeOverseer);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatTheLifeOverseer);
        Assert.Equal(PublicEventObjective.DefeatTheLifeOverseer, activation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_LifeOverseerAirlock_ActivatesMappedObjective()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ReturnToTheAirlockOfTheLifeOverseersCreche);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ReturnToTheAirlockOfTheLifeOverseersCreche);
        Assert.Equal(PublicEventObjective.ReturnToTheAirlockOfTheLifeOverseersCreche, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_LifeOverseerSearch_ActivatesCargoCrateObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(3u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ContinueTheSearchForTheMissingCrewLifeOverseer);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i =>
            (PublicEventObjective)i.Arguments[0] == PublicEventObjective.ContinueTheSearchForTheMissingCrewLifeOverseer
            && (uint)i.Arguments[1] == 3u);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CollectCargoCrate);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.CollectCargoCrate2);
    }

    [Theory]
    [InlineData(PublicEventPhase.FollowTheFriendlySkeech, PublicEventObjective.FollowTheFriendlySkeech)]
    [InlineData(PublicEventPhase.ReturnToTheAirlockOfTheIncubationComplex, PublicEventObjective.ReturnToTheAirlockOfTheIncubationComplex)]
    [InlineData(PublicEventPhase.ReturnToTheAirlockOfTheBiomaticsChamber, PublicEventObjective.ReturnToTheAirlockOfTheBiomaticsChamber)]
    [InlineData(PublicEventPhase.ContinueTheSearchForHugo, PublicEventObjective.ContinueTheSearchForHugo)]
    [InlineData(PublicEventPhase.SearchForHugo, PublicEventObjective.SearchForHugo)]
    [InlineData(PublicEventPhase.ReturnToTheAirlockOfTheLifeOverseersCreche, PublicEventObjective.ReturnToTheAirlockOfTheLifeOverseersCreche)]
    public void OnPublicEventPhase_PlayerCountBranchPhases_ActivateMappedObjective(
        PublicEventPhase phase,
        PublicEventObjective objective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(4u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
        Assert.Equal(objective, activation.Arguments[0]);
        Assert.Equal(4u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_SearchForCrewmateJo_ActivatesSearchAndEggObjectives()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SearchForCrewmateJo);

        List<object> objectives = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .Select(i => i.Arguments[0])
            .ToList();
        Assert.Contains(PublicEventObjective.SearchInsideTheIncubationComplexForJo, objectives);
        Assert.Contains(PublicEventObjective.SmashXenobiteEggsInsideTheIncubationComplex, objectives);
    }

    [Theory]
    [InlineData(PublicEventPhase.SearchForCrewmateJo, 7892u, 9713.49f, -769.372f, -6096.29f)]
    [InlineData(PublicEventPhase.SearchInsideTheBiomaticsChamberForSyrus, 7919u, 10402.6f, -840.416f, -6171.84f)]
    public void OnPublicEventPhase_SearchChamberPhases_CreateWipScriptObjectiveTrigger(
        PublicEventPhase phase,
        uint objectId,
        float x,
        float y,
        float z)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(
            1u,
            out _,
            out RecordingDispatchProxy<IMapInstance> mapProxy,
            out List<RecordingTrigger> createdTriggers);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingTrigger trigger = Assert.Single(createdTriggers);
        Assert.Equal((objectId, 1f), Assert.Single(trigger.GridInitialiseCalls));
        AssertTriggerAddedToMap(mapProxy, trigger, new Vector3(x, y, z));
    }

    [Theory]
    [MemberData(nameof(SearchScriptObjectiveTriggerFilterCases))]
    public void SearchScriptObjectiveTriggerScripts_AreBoundToMappedObjectIds(
        Type scriptType,
        uint expectedOwnerId)
    {
        ScriptFilterOwnerIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterOwnerIdAttribute), inherit: false)
                .Cast<ScriptFilterOwnerIdAttribute>());

        Assert.Equal(new[] { expectedOwnerId }, attribute.Id);
    }

    [Theory]
    [MemberData(nameof(SearchScriptObjectiveTriggerCreditCases))]
    public void SearchScriptObjectiveTriggerScripts_OnEnterRange_UpdateZeroCountScriptObjectiveOnce(
        Type scriptType,
        PublicEventObjective expectedObjective)
    {
        var script = (FragmentZeroSearchGridTriggerScriptBase)Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        IGridEntity nonPlayerEntity = RecordingDispatchProxy<IGridEntity>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateGridTriggerWithPublicEventManager(
            out IGridTriggerEntity trigger);

        script.OnLoad(trigger);
        script.OnEnterRange(nonPlayerEntity);
        script.OnEnterRange(player);
        script.OnEnterRange(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(expectedObjective, update.Arguments[0]);
        Assert.Equal(0, update.Arguments[1]);
    }

    [Theory]
    [InlineData(PublicEventPhase.SearchJoscorpse, PublicEventObjective.SearchJoInsideTheIncubationComplex)]
    [InlineData(PublicEventPhase.SearchCrewmateSyrusCorpse, PublicEventObjective.SearchCrewmateSyrusCorpse)]
    public void OnPublicEventPhase_SearchCorpsePhases_ActivateMappedCorpseObjective(
        PublicEventPhase phase,
        PublicEventObjective objective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
        Assert.Equal(objective, activation.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventPhase.WaitForHugo, PublicEventObjective.WaitForHugo)]
    [InlineData(PublicEventPhase.StayCloseToHugo, PublicEventObjective.StayCloseToHugo)]
    public void OnPublicEventPhase_FinalHugoZeroCountBridge_ActivatesAndCreditsObjective(
        PublicEventPhase phase,
        PublicEventObjective objective)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(
            eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(objective, update.Arguments[0]);
        Assert.Equal(0, update.Arguments[1]);
    }

    [Theory]
    [InlineData(PublicEventPhase.FollowTheFriendlySkeech, CommunicatorMessage.SupervisorLola)]
    [InlineData(PublicEventPhase.SurviveTheSkeechAmbush, CommunicatorMessage.SupervisorLolax)]
    public void OnPublicEventPhase_BranchMessagePhases_BroadcastMappedSupervisorMessage(PublicEventPhase phase, CommunicatorMessage messageId)
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((messageId, message));
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Success_AdvancesBranchPhaseChain()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SearchInsideTheBiomaticsChamberForSyrus, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.DefeatProjectMatron);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DefeatProjectMatronSuccess_SetsSearchCrewmateSyrusCorpsePhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatProjectMatron, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.SearchCrewmateSyrusCorpse);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_DefeatTheLifeOverseerSuccess_SetsReturnAirlockPhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatTheLifeOverseer, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == PublicEventPhase.ReturnToTheAirlockOfTheLifeOverseersCreche);
    }

    [Theory]
    [InlineData(PublicEventObjective.ReturnToTheAirlockOfTheLifeOverseersCreche, PublicEventPhase.LocateHugo)]
    [InlineData(PublicEventObjective.LocateHugo, PublicEventPhase.WaitForHugo)]
    [InlineData(PublicEventObjective.WaitForHugo, PublicEventPhase.SeeIfHugoHasAWayOutOfThisMess)]
    [InlineData(PublicEventObjective.SeeIfHugoHasAWayOutOfThisMess, PublicEventPhase.StayCloseToHugo)]
    [InlineData(PublicEventObjective.StayCloseToHugo, PublicEventPhase.SpeakWithHugo)]
    public void OnPublicEventObjectiveStatus_FinalHugoBridgeSuccess_AdvancesPhase(
        PublicEventObjective objective,
        PublicEventPhase phase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == phase);
    }

    [Fact]
    public void CaptainHugoLocateScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(CaptainHugoLocateEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 67961u }, attribute.CreatureId);
    }

    [Fact]
    public void CaptainHugoLocateScript_OnActivateSuccess_UpdatesMappedTargetGroupOnce()
    {
        var script = new CaptainHugoLocateEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity hugo);

        script.OnLoad(hugo);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(12267u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalObjective_CreditsGoldTimerAndFinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SpeakWithHugo, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation update = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.UpdateObjective)));
        Assert.Equal(PublicEventObjective.GoldMedalTimer, update.Arguments[0]);
        Assert.Equal(0, update.Arguments[1]);

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Theory]
    [MemberData(nameof(HugoTalkObjectiveFilterCases))]
    public void HugoTalkObjectiveScripts_AreBoundToMappedCreatures(
        Type scriptType,
        uint expectedCreatureId)
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { expectedCreatureId }, attribute.CreatureId);
    }

    [Theory]
    [MemberData(nameof(HugoTalkObjectiveCreditCases))]
    public void HugoTalkObjectiveScripts_OnActivateSuccess_UpdatesMappedTalkTargetGroup(
        Type scriptType,
        uint expectedTargetGroupId)
    {
        var script = (FragmentZeroTalkObjectiveEntityScriptBase)Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity hugo);

        script.OnLoad(hugo);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Same(player, update.Arguments[0]);
        Assert.Equal(PublicEventObjectiveType.TalkTo, update.Arguments[1]);
        Assert.Equal(expectedTargetGroupId, update.Arguments[2]);
        Assert.Equal(1, update.Arguments[3]);
    }

    [Fact]
    public void FacilityDefenseControlPanelScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(FacilityDefenseControlPanelEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 68856u }, attribute.CreatureId);
    }

    [Fact]
    public void FacilityDefenseControlPanel_OnActivateSuccess_UpdatesActiveChecklistTargetGroupObjectiveOnce()
    {
        var script = new FacilityDefenseControlPanelEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity panel);

        script.OnLoad(panel);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroupChecklist, update.Arguments[0]);
        Assert.Equal(12461u, update.Arguments[1]);
        Assert.Equal(0, update.Arguments[2]);
    }

    [Theory]
    [MemberData(nameof(PrototypeObjectiveCreditFilterCases))]
    public void PrototypeObjectiveCreditScripts_AreBoundToMappedCreatures(
        Type scriptType,
        uint[] expectedCreatureIds)
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(expectedCreatureIds, attribute.CreatureId);
    }

    [Fact]
    public void CargoCrateScript_IsBoundToMappedCreature()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(CargoCrateEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 67965u }, attribute.CreatureId);
    }

    [Fact]
    public void CargoCrate_OnActivateSuccess_UpdatesVirtualCollectAndRemovesOnce()
    {
        var script = new CargoCrateEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity crate,
            out RecordingDispatchProxy<IWorldEntity> crateProxy);

        script.OnLoad(crate);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.VirtualCollect, update.Arguments[0]);
        Assert.Equal(1176u, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
        Assert.Single(crateProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void XenobiteEggScript_IsBoundToMappedCreatures()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(XenobiteEggEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 68811u, 68812u }, attribute.CreatureId);
    }

    [Fact]
    public void XenobiteEgg_OnActivateSuccess_UpdatesScriptObjectiveAndRemovesOnce()
    {
        var script = new XenobiteEggEntityScript();
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(
            out IWorldEntity egg,
            out RecordingDispatchProxy<IWorldEntity> eggProxy);

        script.OnLoad(egg);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjective.SmashXenobiteEggsInsideTheIncubationComplex, update.Arguments[0]);
        Assert.Equal(1, update.Arguments[1]);
        Assert.Single(eggProxy.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void FragmentZeroSkeechHordeScript_IsBoundToReviewedTargetGroupCreatures()
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            typeof(FragmentZeroSkeechHordeEntityScript).GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { 67516u, 67518u, 67519u, 67520u }, attribute.CreatureId);
    }

    [Fact]
    public void FragmentZeroSkeechHordeScript_OnDeath_UpdatesMappedBaseAndTierObjectivesOnce()
    {
        var script = new FragmentZeroSkeechHordeEntityScript();
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateCreatureWithPublicEventManager(out ICreatureEntity skeech);

        script.OnLoad(skeech);
        script.OnDeath();
        script.OnDeath();

        List<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates = publicEventManagerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .ToList();

        Assert.Equal(4, updates.Count);
        Assert.Contains(updates, update =>
            update.Arguments[0] is PublicEventObjective objective
            && objective == PublicEventObjective.EliminateSkeech
            && (int)update.Arguments[1] == 1);
        Assert.Contains(updates, update =>
            update.Arguments[0] is PublicEventObjective objective
            && objective == PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea
            && (int)update.Arguments[1] == 1);
        Assert.Contains(updates, update =>
            update.Arguments[0] is PublicEventObjective objective
            && objective == PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea2
            && (int)update.Arguments[1] == 1);
        Assert.Contains(updates, update =>
            update.Arguments[0] is PublicEventObjective objective
            && objective == PublicEventObjective.EliminateTheHordesOfSkeechInfestingTheArea3
            && (int)update.Arguments[1] == 1);
    }

    [Theory]
    [MemberData(nameof(CorpseSearchFilterCases))]
    public void CorpseSearchScripts_AreBoundToMappedCreatures(
        Type scriptType,
        uint expectedCreatureId)
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { expectedCreatureId }, attribute.CreatureId);
    }

    [Theory]
    [MemberData(nameof(CorpseSearchObjectiveCreditCases))]
    public void CorpseSearchScripts_OnActivateSuccess_UpdateMappedTargetGroupOnce(
        Type scriptType,
        uint expectedTargetGroupId)
    {
        var script = (FragmentZeroTargetGroupObjectiveEntityScriptBase)Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity corpse);

        script.OnLoad(corpse);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        RecordingDispatchProxy<IPublicEventManager>.Invocation update = Assert.Single(
            publicEventManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(PublicEventObjectiveType.ActivateTargetGroup, update.Arguments[0]);
        Assert.Equal(expectedTargetGroupId, update.Arguments[1]);
        Assert.Equal(1, update.Arguments[2]);
    }

    [Theory]
    [MemberData(nameof(HiddenRecordingFilterCases))]
    public void HiddenRecordingScripts_AreBoundToMappedCreatures(
        Type scriptType,
        uint expectedCreatureId)
    {
        ScriptFilterCreatureIdAttribute attribute = Assert.Single(
            scriptType.GetCustomAttributes(typeof(ScriptFilterCreatureIdAttribute), inherit: false)
                .Cast<ScriptFilterCreatureIdAttribute>());

        Assert.Equal(new[] { expectedCreatureId }, attribute.CreatureId);
    }

    [Theory]
    [MemberData(nameof(HiddenRecordingObjectiveCreditCases))]
    public void HiddenRecordingScripts_OnActivateSuccess_UpdateChildChecklistAndAggregateOnce(
        Type scriptType,
        uint expectedTargetGroupId)
    {
        var script = (FragmentZeroTargetGroupObjectiveEntityScriptBase)Activator.CreateInstance(scriptType);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out _);
        RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy = CreateWorldEntityWithPublicEventManager(out IWorldEntity recording);

        script.OnLoad(recording);
        script.OnActivateSuccess(player);
        script.OnActivateSuccess(player);

        List<RecordingDispatchProxy<IPublicEventManager>.Invocation> updates = publicEventManagerProxy
            .GetInvocations(nameof(IPublicEventManager.UpdateObjective))
            .ToList();

        Assert.Equal(2, updates.Count);
        Assert.Contains(updates, update =>
            update.Arguments[0] is PublicEventObjectiveType objectiveType
            && objectiveType == PublicEventObjectiveType.ActivateTargetGroupChecklist
            && (uint)update.Arguments[1] == expectedTargetGroupId
            && (int)update.Arguments[2] == 0);
        Assert.Contains(updates, update =>
            update.Arguments[0] is PublicEventObjective objective
            && objective == PublicEventObjective.DiscoverTheTwoLostRecordings
            && (int)update.Arguments[1] == 1);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.ContinueTheSearchForTheMissingCrew, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void OnCinematicFinish_SearchContinuation_SendsCaptainHugoMessageToPlayer()
    {
        IPlayer player = CreatePlayer(out IGameSession session);
        ICommunicatorMessage message = RecordingDispatchProxy<ICommunicatorMessage>.Create(out RecordingDispatchProxy<ICommunicatorMessage> messageProxy);
        var script = CreateScript((CommunicatorMessage.CaptainHugo1, message));
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out RecordingDispatchProxy<IPublicEvent> eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Phase), (uint)PublicEventPhase.ContinueTheSearchForTheMissingCrew);
        script.OnLoad(publicEvent);

        script.OnCinematicFinish(player, 0u);

        RecordingDispatchProxy<ICommunicatorMessage>.Invocation send = Assert.Single(messageProxy.GetInvocations(nameof(ICommunicatorMessage.Send)));
        Assert.Same(session, send.Arguments[0]);
    }

    public static IEnumerable<object[]> HugoTalkObjectiveFilterCases()
    {
        yield return [typeof(CaptainHugoAuraEntityScript), 68886u];
        yield return [typeof(CaptainHugoEventEndEntityScript), 68854u];
    }

    public static IEnumerable<object[]> HugoTalkObjectiveCreditCases()
    {
        yield return [typeof(CaptainHugoAuraEntityScript), 12471u];
        yield return [typeof(CaptainHugoEventEndEntityScript), 12458u];
    }

    public static IEnumerable<object[]> HiddenRecordingFilterCases()
    {
        yield return [typeof(OhmnasHiddenRecordingEntityScript), 67967u];
        yield return [typeof(LucentsHiddenRecordingEntityScript), 67968u];
    }

    public static IEnumerable<object[]> CorpseSearchFilterCases()
    {
        yield return [typeof(CrewmateJoCorpseEntityScript), 67966u];
        yield return [typeof(CrewmateSyrusCorpseEntityScript), 67931u];
    }

    public static IEnumerable<object[]> CorpseSearchObjectiveCreditCases()
    {
        yield return [typeof(CrewmateJoCorpseEntityScript), 12274u];
        yield return [typeof(CrewmateSyrusCorpseEntityScript), 12264u];
    }

    public static IEnumerable<object[]> HiddenRecordingObjectiveCreditCases()
    {
        yield return [typeof(OhmnasHiddenRecordingEntityScript), 12276u];
        yield return [typeof(LucentsHiddenRecordingEntityScript), 12279u];
    }

    public static IEnumerable<object[]> PrototypeObjectiveCreditFilterCases()
    {
        yield return [typeof(PrototypeAlphaEntityScript), new uint[] { 67526u, 69672u }];
        yield return [typeof(PrototypeBetaEntityScript), new uint[] { 67527u, 69673u }];
        yield return [typeof(PrototypeDeltaEntityScript), new uint[] { 67528u, 69674u }];
    }

    public static IEnumerable<object[]> SearchScriptObjectiveTriggerFilterCases()
    {
        yield return [typeof(FragmentZeroIncubationSearchGridTriggerScript), 7892u];
        yield return [typeof(FragmentZeroBiomaticsSearchGridTriggerScript), 7919u];
    }

    public static IEnumerable<object[]> SearchScriptObjectiveTriggerCreditCases()
    {
        yield return [typeof(FragmentZeroIncubationSearchGridTriggerScript), PublicEventObjective.SearchInsideTheIncubationComplexForJo];
        yield return [typeof(FragmentZeroBiomaticsSearchGridTriggerScript), PublicEventObjective.SearchInsideTheBiomaticsChamberForSyrus];
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, IReadOnlyList<IPlayer> players, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, players, out eventProxy, out _, out _);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<RecordingTrigger> createdTriggers)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy, out mapProxy, out createdTriggers);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<RecordingTrigger> createdTriggers)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3180u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<RecordingTrigger> triggers = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            var trigger = new RecordingTrigger();
            triggers.Add(trigger);
            return trigger;
        });

        createdTriggers = triggers;
        return publicEvent;
    }

    private static IPublicEvent CreatePublicEventWithReviewedSupervisorLola(
        uint playerCount,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<RecordingTrigger> createdTriggers,
        out List<CreatedNpc> createdNpcs)
    {
        return CreatePublicEventWithReviewedSupervisorLola(playerCount, [], out eventProxy, out mapProxy, out createdTriggers, out createdNpcs);
    }

    private static IPublicEvent CreatePublicEventWithReviewedSupervisorLola(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy,
        out RecordingDispatchProxy<IMapInstance> mapProxy,
        out List<RecordingTrigger> createdTriggers,
        out List<CreatedNpc> createdNpcs)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetProperty(nameof(IMap.Entry), new WorldEntry { Id = 3180u });
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);

        List<RecordingTrigger> triggers = [];
        List<CreatedNpc> npcs = [];
        eventProxy.SetMethodReturnFactory(nameof(IPublicEvent.CreateEntity), () =>
        {
            if (npcs.Count == 0)
            {
                CreatedNpc npc = CreateNpc();
                npcs.Add(npc);
                return npc.Instance;
            }

            var trigger = new RecordingTrigger();
            triggers.Add(trigger);
            return trigger;
        });

        createdTriggers = triggers;
        createdNpcs = npcs;
        return publicEvent;
    }

    private static CreatedNpc CreateNpc()
    {
        INonPlayerEntity npc = RecordingDispatchProxy<INonPlayerEntity>.Create(
            out RecordingDispatchProxy<INonPlayerEntity> npcProxy);
        return new CreatedNpc(npc, npcProxy);
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

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(out IWorldEntity entity)
    {
        return CreateWorldEntityWithPublicEventManager(out entity, out _);
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateWorldEntityWithPublicEventManager(
        out IWorldEntity entity,
        out RecordingDispatchProxy<IWorldEntity> entityProxy)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<IWorldEntity>.Create(out entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        return publicEventManagerProxy;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateCreatureWithPublicEventManager(out ICreatureEntity entity)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        entity = RecordingDispatchProxy<ICreatureEntity>.Create(out RecordingDispatchProxy<ICreatureEntity> entityProxy);
        entityProxy.SetProperty(nameof(IGridEntity.Map), map);
        return publicEventManagerProxy;
    }

    private static RecordingDispatchProxy<IPublicEventManager> CreateGridTriggerWithPublicEventManager(out IGridTriggerEntity trigger)
    {
        IPublicEventManager publicEventManager = RecordingDispatchProxy<IPublicEventManager>.Create(out RecordingDispatchProxy<IPublicEventManager> publicEventManagerProxy);
        IBaseMap map = RecordingDispatchProxy<IBaseMap>.Create(out RecordingDispatchProxy<IBaseMap> mapProxy);
        mapProxy.SetProperty(nameof(IBaseMap.PublicEventManager), publicEventManager);

        trigger = RecordingDispatchProxy<IGridTriggerEntity>.Create(out RecordingDispatchProxy<IGridTriggerEntity> triggerProxy);
        triggerProxy.SetProperty(nameof(IGridEntity.Map), map);
        return publicEventManagerProxy;
    }

    private static FragmentZeroEventScript CreateScript(params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        return CreateScript(RecordingDispatchProxy<ICinematicFactory>.Create(out _), messages);
    }

    private static FragmentZeroEventScript CreateScript(
        ICinematicFactory cinematicFactory,
        params (CommunicatorMessage Id, ICommunicatorMessage Message)[] messages)
    {
        IReadOnlyDictionary<CommunicatorMessage, ICommunicatorMessage> lookup = messages.ToDictionary(m => m.Id, m => m.Message);
        IGlobalQuestManager globalQuestManager = RecordingDispatchProxy<IGlobalQuestManager>.Create(out RecordingDispatchProxy<IGlobalQuestManager> globalQuestManagerProxy);
        globalQuestManagerProxy.SetMethodHandler(nameof(IGlobalQuestManager.GetCommunicatorMessage),
            args => lookup.TryGetValue((CommunicatorMessage)args[0], out ICommunicatorMessage message) ? message : null);
        return new FragmentZeroEventScript(globalQuestManager, cinematicFactory);
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

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        RecordingTrigger trigger,
        Vector3 expectedPosition)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd = Assert.Single(mapProxy.GetInvocations(nameof(IMap.EnqueueAdd)));
        Assert.Same(trigger, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3180u, position.Info.Entry.Id);
    }

    private static void AssertTriggerAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        RecordingTrigger trigger,
        Vector3 expectedPosition,
        int invocationIndex)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd =
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd))[invocationIndex];
        Assert.Same(trigger, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3180u, position.Info.Entry.Id);
    }

    private static void AssertEntityAddedToMap(
        RecordingDispatchProxy<IMapInstance> mapProxy,
        IGridEntity entity,
        Vector3 expectedPosition,
        int invocationIndex)
    {
        RecordingDispatchProxy<IMapInstance>.Invocation enqueueAdd =
            mapProxy.GetInvocations(nameof(IMap.EnqueueAdd))[invocationIndex];
        Assert.Same(entity, enqueueAdd.Arguments[0]);

        IMapPosition position = Assert.IsAssignableFrom<IMapPosition>(enqueueAdd.Arguments[1]);
        Assert.Equal(expectedPosition, position.Position);
        Assert.Equal(3180u, position.Info.Entry.Id);
    }

    private static void AssertSupervisorLolaModel(CreatedNpc npc)
    {
        RecordingDispatchProxy<INonPlayerEntity>.Invocation initialise = Assert.Single(
            npc.Proxy.GetInvocations(nameof(IWorldEntity.Initialise)));
        EntityModel model = Assert.IsType<EntityModel>(initialise.Arguments[0]);
        Assert.Equal(1100300015u, model.Id);
        Assert.Equal(EntityType.NonPlayer, model.Type);
        Assert.Equal(71762u, model.Creature);
        Assert.Equal((ushort)3180u, model.World);
        Assert.Equal((ushort)4619u, model.Area);
        Assert.Equal(9873.75f, model.X);
        Assert.Equal(-765.9531f, model.Y);
        Assert.Equal(-5848.58f, model.Z);
        Assert.Equal(0f, model.Rx);
        Assert.Equal(0f, model.Ry);
        Assert.Equal(0f, model.Rz);
        Assert.Equal(32807u, model.DisplayInfo);
        Assert.Equal((ushort)0u, model.OutfitInfo);
        Assert.Equal((ushort)219u, model.Faction1);
        Assert.Equal((ushort)219u, model.Faction2);
        Assert.Null(model.EntityEvent);
        Assert.Empty(model.EntityScript);
        Assert.Equal(3, model.EntityStat.Count);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Health && stat.Value == 1f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Shield && stat.Value == 0f);
        Assert.Contains(model.EntityStat, stat => stat.Stat == (byte)Stat.Unknown22 && stat.Value == 0f);
    }

    private sealed record CreatedNpc(
        INonPlayerEntity Instance,
        RecordingDispatchProxy<INonPlayerEntity> Proxy);

    private sealed class RecordingTrigger : IWorldLocationVolumeGridTriggerEntity, ITurnstileGridTriggerEntity
    {
        public List<(uint WorldLocationId, uint ObjectId)> WorldLocationInitialiseCalls { get; } = [];
        public List<(uint TriggerId, float Range, uint ObjectId)> TurnstileInitialiseCalls { get; } = [];
        public List<(uint Id, float Range)> GridInitialiseCalls { get; } = [];

        public WorldLocation2Entry Entry { get; } = new();
        public uint Guid => 0u;
        public IBaseMap Map => null;
        public Vector3 Position => default;
        public IMapInfo PreviousMap => null;
        public bool InWorld => false;
        public float ActivationRange => 0f;
        public float? RangeCheck => null;

        public void Initialise(uint worldLocationId, uint objectId)
        {
            WorldLocationInitialiseCalls.Add((worldLocationId, objectId));
        }

        public void Initialise(uint id, float range, uint objectId)
        {
            TurnstileInitialiseCalls.Add((id, range, objectId));
        }

        public void Initialise(uint id, float range)
        {
            GridInitialiseCalls.Add((id, range));
        }

        public Task<T> SynchroniseAsync<T>(Func<T> func)
        {
            return Task.FromResult(func());
        }

        public void InvokeScriptCollection<T>(Action<T> action)
        {
        }

        public void RemoveFromMap()
        {
        }

        public void Relocate(Vector3 position)
        {
        }

        public void OnEnqueueAddToMap()
        {
        }

        public void OnAddToMap(IBaseMap map, uint guid, Vector3 vector)
        {
        }

        public void OnEnqueueRemoveFromMap()
        {
        }

        public void OnRemoveFromMap()
        {
        }

        public void OnRelocate(Vector3 vector)
        {
        }

        public bool CanSeeEntity(IGridEntity entity)
        {
            return false;
        }

        public void AddVisible(IGridEntity entity)
        {
        }

        public void RemoveVisible(IGridEntity entity)
        {
        }

        public T GetVisible<T>(uint guid) where T : IGridEntity
        {
            return default;
        }

        public IEnumerable<T> GetVisibleCreature<T>(uint creatureId) where T : IWorldEntity
        {
            return [];
        }

        public IPlayer GetVisiblePlayer(Identity identity)
        {
            return null;
        }

        public void SetInRangeCheck(float range)
        {
        }

        public void CheckEntityInRange(IGridEntity target)
        {
        }

        public IEnumerable<T> GetInRange<T>(uint guid) where T : IGridEntity
        {
            return [];
        }

        public void Update(double lastTick)
        {
        }

        public void Dispose()
        {
        }
    }
}
