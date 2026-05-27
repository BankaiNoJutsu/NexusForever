using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Dungeon.RuinsOfKelVoreth;

namespace NexusForever.Game.Tests.Instances;

public class RuinsOfKelVorethEventScriptTests
{
    [Fact]
    public void OnLoad_SetsInitialBloodPitPhase()
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);

        script.OnLoad(publicEvent);

        RecordingDispatchProxy<IPublicEvent>.Invocation invocation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Equal(PublicEventPhase.FightInBloodPit, invocation.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventPhase_BloodPit_ActivatesBranchMappedCount()
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.FightInBloodPit);

        RecordingDispatchProxy<IPublicEvent>.Invocation activation = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)));
        Assert.Equal(PublicEventObjective.FightYourWayThroughTheBloodPit, activation.Arguments[0]);
        Assert.Equal(3u, activation.Arguments[1]);
    }

    [Fact]
    public void OnPublicEventPhase_SlaveMaster_ActivatesBranchPrimaryAndChallengeObjectives()
    {
        var script = new NoOptionalRuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SlaveMasterDrokk);

        List<RecordingDispatchProxy<IPublicEvent>.Invocation> activations = eventProxy
            .GetInvocations(nameof(IPublicEvent.ActivateObjective))
            .ToList();
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatSlavemasterDrokk);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DefeatDarkwitchGurka);
        Assert.Contains(activations, i => (PublicEventObjective)i.Arguments[0] == PublicEventObjective.DodgingTheDefense);
    }

    [Fact]
    public void OnPublicEventPhase_SlaveMaster_ActivatesBranchOptionalObjectives()
    {
        var script = new AllOptionalRuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.SlaveMasterDrokk);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.PutTheKelVorethSlavesOutOfTheirMisery);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.AccessTheHiddenEldanDataStorageDevices);
    }

    [Fact]
    public void OnPublicEventPhase_ForgeMaster_ActivatesBranchOptionalObjectives()
    {
        var script = new AllOptionalRuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventPhase((uint)PublicEventPhase.ForgeMasterTrogun);

        AssertObjectiveActivated(eventProxy, PublicEventObjective.DefeatForgemasterTrogun);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.DestroyKelVorethForges);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.KillBattleswornAndDarkwitchOsun);
        AssertObjectiveActivated(eventProxy, PublicEventObjective.BurnKelVorethWarSupplies);
    }

    [Theory]
    [InlineData(PublicEventObjective.FightYourWayThroughTheBloodPit, PublicEventPhase.GrondTheCorpsemaker)]
    [InlineData(PublicEventObjective.DefeatGrondTheCorpsemaker, PublicEventPhase.SlaveMasterDrokk)]
    [InlineData(PublicEventObjective.DefeatSlavemasterDrokk, PublicEventPhase.ForgeMasterTrogun)]
    public void OnPublicEventObjectiveStatus_Success_AdvancesMainBossChain(PublicEventObjective objective, PublicEventPhase nextPhase)
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(objective, PublicEventStatus.Succeeded));

        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)),
            i => (PublicEventPhase)i.Arguments[0] == nextPhase);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_FinalBoss_FinishesEvent()
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.DefeatForgemasterTrogun, PublicEventStatus.Succeeded));

        RecordingDispatchProxy<IPublicEvent>.Invocation finish = Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
        Assert.Equal(PublicEventTeam.PublicTeam, finish.Arguments[0]);
    }

    [Fact]
    public void OnPublicEventObjectiveStatus_Incomplete_DoesNotAdvance()
    {
        var script = new RuinsOfKelVorethEventScript();
        IPublicEvent publicEvent = CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy);
        script.OnLoad(publicEvent);

        script.OnPublicEventObjectiveStatus(CreateObjective(PublicEventObjective.FightYourWayThroughTheBloodPit, PublicEventStatus.Active));

        Assert.Single(eventProxy.GetInvocations(nameof(IPublicEvent.SetPhase)));
        Assert.Empty(eventProxy.GetInvocations(nameof(IPublicEvent.Finish)));
    }

    private static IPublicEvent CreatePublicEvent(out RecordingDispatchProxy<IPublicEvent> eventProxy)
    {
        IMapInstance mapInstance = RecordingDispatchProxy<IMapInstance>.Create(out _);

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

    private static void AssertObjectiveActivated(RecordingDispatchProxy<IPublicEvent> eventProxy, PublicEventObjective objective)
    {
        Assert.Contains(eventProxy.GetInvocations(nameof(IPublicEvent.ActivateObjective)),
            i => (PublicEventObjective)i.Arguments[0] == objective);
    }

    private sealed class NoOptionalRuinsOfKelVorethEventScript : RuinsOfKelVorethEventScript
    {
        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return false;
        }
    }

    private sealed class AllOptionalRuinsOfKelVorethEventScript : RuinsOfKelVorethEventScript
    {
        protected override bool ShouldActivateWipOptionalObjective(PublicEventObjective objective)
        {
            return true;
        }
    }
}
