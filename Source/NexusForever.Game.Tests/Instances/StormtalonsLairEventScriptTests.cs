using NexusForever.Game.Abstract.Cinematic;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Dungeon.StormtalonsLair;

namespace NexusForever.Game.Tests.Instances;

public class StormtalonsLairEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialEnterPhase()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.Enter, invocation.Arguments[0]);
    }

    [Theory]
    [InlineData(PublicEventPhase.Enter, PublicEventObjective.SurviveTheThundercallPellZealots)]
    [InlineData(PublicEventPhase.DefeatBladeWindTheInvoker, PublicEventObjective.DefeatBladeWindTheInvoker)]
    [InlineData(PublicEventPhase.EliminateAethros, PublicEventObjective.EliminateAethros)]
    [InlineData(PublicEventPhase.DestroyStormtalon, PublicEventObjective.DestroyStormtalon)]
    public void OnPublicEventPhase_MainPhases_ActivateMappedObjective(PublicEventPhase phase, PublicEventObjective objective)
    {
        var script = new NoOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(2u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)phase);

        AssertObjectiveActivated(eventProxy, objective);
    }

    [Fact]
    public void OnPublicEventPhase_Enter_ActivatesBranchOptionalGatherObjective()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.Enter);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.SurviveTheThundercallPellZealots);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.GatherTaintedStemSamples);
    }

    [Fact]
    public void OnPublicEventPhase_BladeWind_ActivatesBranchOptionalSideObjectives()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DefeatBladeWindTheInvoker);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatBladeWindTheInvoker);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.ObtainDataFromTheThundercallDataAltar);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.HijackPowerFromTheThundercallStormTotems);
    }

    [Fact]
    public void OnPublicEventPhase_Aethros_ActivatesBranchOptionalArcanistRoute()
    {
        var script = new AllOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EliminateAethros);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.EliminateAethros);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.FreeTheThundercallSacrificialPrisoners);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.UseYourGrenadesToDisableTheThundercallPell);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatArcanistBreezeBinderForTheEncryptionKey);
    }

    [Fact]
    public void OnPublicEventPhase_Aethros_CanActivateBranchOverseerRoute()
    {
        var script = new OverseerOptionalStormtalonsLairEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.EliminateAethros);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillOverseerDriftCatcher);
    }

    [Fact]
    public void OnPublicEventPhase_StopHighPriest_UsesCurrentPlayerCount()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(4u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.StopTheThundercallHighPriest);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.StopTheThundercallHighPriest, activation.Arguments[0]);
        Assert.Equal(4u, activation.Arguments[1]);
    }

    [Theory]
    [InlineData(PublicEventObjective.SurviveTheThundercallPellZealots, PublicEventPhase.DefeatBladeWindTheInvoker)]
    [InlineData(PublicEventObjective.DefeatBladeWindTheInvoker, PublicEventPhase.EliminateAethros)]
    [InlineData(PublicEventObjective.EliminateAethros, PublicEventPhase.StopTheThundercallHighPriest)]
    [InlineData(PublicEventObjective.StopTheThundercallHighPriest, PublicEventPhase.DestroyStormtalon)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesMainChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DestroyStormtalon, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = CreateScript();
        IPublicEvent publicEvent = CreatePublicEvent(1u, out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.SurviveTheThundercallPellZealots, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    [Fact]
    public void OnPublicEventPhase_DestroyStormtalon_QueuesWipRebornCinematic()
    {
        ICinematicBase cinematic = RecordingDispatchProxy<ICinematicBase>.Create(out _);
        IPlayer player = CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy);
        var script = CreateScript(cinematic);
        IPublicEvent publicEvent = CreatePublicEvent(1u, [player], out _);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.DestroyStormtalon);

        RecordingDispatchProxy<ICinematicManager>.Invocation queue = Assert.Single(
            cinematicManagerProxy.GetInvocations(nameof(ICinematicManager.QueueCinematic)));
        Assert.Same(cinematic, queue.Arguments[0]);
    }

    private static IPublicEvent CreatePublicEvent(uint playerCount, out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        return CreatePublicEvent(playerCount, [], out eventProxy);
    }

    private static IPublicEvent CreatePublicEvent(
        uint playerCount,
        IReadOnlyList<IPlayer> players,
        out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out RecordingDispatchProxy<IMapInstance> mapProxy);
        mapProxy.SetProperty(nameof(IMapInstance.PlayerCount), playerCount);
        mapProxy.SetMethodReturn(nameof(IMapInstance.GetPlayers), players);

        IPublicEvent publicEvent = RecordingDispatchProxy<IPublicEvent>.Create(out eventProxy);
        eventProxy.SetProperty(nameof(IPublicEvent.Map), mapInstance);
        return publicEvent;
    }

    private static StormtalonsLairEventScript CreateScript()
    {
        return CreateScript(RecordingDispatchProxy<ICinematicBase>.Create(out _));
    }

    private static StormtalonsLairEventScript CreateScript(ICinematicBase cinematic)
    {
        return new StormtalonsLairEventScript(CreateCinematicFactory(cinematic));
    }

    private static ICinematicFactory CreateCinematicFactory(ICinematicBase cinematic)
    {
        ICinematicFactory cinematicFactory = RecordingDispatchProxy<ICinematicFactory>.Create(out RecordingDispatchProxy<ICinematicFactory> cinematicFactoryProxy);
        cinematicFactoryProxy.SetMethodReturn(nameof(ICinematicFactory.CreateCinematic), cinematic);
        return cinematicFactory;
    }

    private static IPlayer CreatePlayer(out RecordingDispatchProxy<ICinematicManager> cinematicManagerProxy)
    {
        ICinematicManager cinematicManager = RecordingDispatchProxy<ICinematicManager>.Create(out cinematicManagerProxy);
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out RecordingDispatchProxy<IPlayer> playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CinematicManager), cinematicManager);
        return player;
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

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
    }

    private class NoOptionalStormtalonsLairEventScript : StormtalonsLairEventScript
    {
        public NoOptionalStormtalonsLairEventScript()
            : base(CreateCinematicFactory(RecordingDispatchProxy<ICinematicBase>.Create(out _)))
        {
        }

        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return false;
        }

        protected override bool ShouldUseWipArcanistVariant()
        {
            return false;
        }
    }

    private class AllOptionalStormtalonsLairEventScript : StormtalonsLairEventScript
    {
        public AllOptionalStormtalonsLairEventScript()
            : base(CreateCinematicFactory(RecordingDispatchProxy<ICinematicBase>.Create(out _)))
        {
        }

        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return true;
        }

        protected override bool ShouldUseWipArcanistVariant()
        {
            return true;
        }
    }

    private sealed class OverseerOptionalStormtalonsLairEventScript : AllOptionalStormtalonsLairEventScript
    {
        protected override bool ShouldUseWipArcanistVariant()
        {
            return false;
        }
    }
}
