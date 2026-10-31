using System.Numerics;
using System.Reflection;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Map;
using NexusForever.Game.Abstract.Map.Instance;
using NexusForever.Game.Abstract.PublicEvent;
using NexusForever.Game.Entity;
using NexusForever.Game.Static.PublicEvent;
using NexusForever.Game.Tests.PublicEvents;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Instance.Adventure.TheHycrestInsurrection;
using NexusForever.Script.Instance.Adventure.TheHycrestInsurrection.Script;
using NexusForever.Script.Template.Filter;
using NexusForever.Shared;
using HycrestObjective = NexusForever.Script.Instance.Adventure.TheHycrestInsurrection.PublicEventObjective;

namespace NexusForever.Game.Tests.Instances;

public class HycrestReconciliationTests
{
    [Theory]
    [InlineData(PublicEventObjectiveType.TimedWin, PublicEventStatus.Succeeded)]
    [InlineData(PublicEventObjectiveType.Script, PublicEventStatus.Failed)]
    public void RealTimerHonorsTwentySecondBoundaryBusyAndExactlyOnceCompletion(PublicEventObjectiveType type, PublicEventStatus expected)
    {
        var objective = PublicEventTestSupport.CreateObjective(new PublicEventObjectiveEntry
        { Id = 2155, PublicEventId = 418, PublicEventObjectiveTypeEnum = type, Count = 1, FailureTimeMs = 20000,
            PublicEventTeamId = PublicEventTeam.PublicTeam });
        var owner = (RecordingDispatchProxy<IPublicEvent>)(object)objective.Team.PublicEvent;
        objective.Update(100);
        Assert.Equal(PublicEventStatus.Inactive, objective.Status);
        objective.EnableTimedWinCompletion();
        objective.ActivateObjective();
        objective.Update(19.999);
        objective.SetBusy(true);
        objective.Update(10);
        Assert.Equal(PublicEventStatus.Active, objective.Status);
        objective.SetBusy(false);
        objective.Update(0.00101);
        Assert.Equal(expected, objective.Status);
        int callbacks = owner.GetInvocations(nameof(IPublicEvent.InvokeScriptCollection)).Count;
        objective.Update(30);
        Assert.Equal(callbacks, owner.GetInvocations(nameof(IPublicEvent.InvokeScriptCollection)).Count);
        Assert.Equal(0u, objective.Count);
        if (expected == PublicEventStatus.Succeeded)
        {
            objective.ResetObjective();
            objective.ActivateObjective();
            objective.Update(19);
            Assert.Equal(PublicEventStatus.Active, objective.Status);
            objective.Update(1);
            Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        }
    }

    [Fact]
    public void ChangingActiveParticipantMaxBroadcastsCurrentStateAndCompletesOnlyAtBound()
    {
        var objective = MeetObjective(2);
        objective.UpdateObjective(1);
        Assert.Equal(PublicEventStatus.Active, objective.Status);
        objective.SetDynamicMax(3);
        Assert.Equal(3u, objective.Build().ObjectiveStatus.DynamicMax);
        Assert.Equal(1u, objective.Count);
        objective.SetDynamicMax(1);
        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        objective.SetDynamicMax(4);
        Assert.Equal(1u, objective.DynamicMax);
    }

    [Fact]
    public void HycrestBriefingControllerOptsInActualTimerWithoutIssuingCounterCredit()
    {
        var f = new Fixture();
        var objective = PublicEventTestSupport.CreateObjective(new PublicEventObjectiveEntry
        { Id = 2155, PublicEventId = 418, PublicEventTeamId = PublicEventTeam.PublicTeam,
            PublicEventObjectiveTypeEnum = PublicEventObjectiveType.TimedWin, FailureTimeMs = 20000, Count = 1 });
        f.SetMeetObjective(objective);
        f.EventProxy(418).SetMethodHandler(nameof(IPublicEvent.ActivateObjective), args => { objective.ActivateObjective(); return null; });
        f.Intro.OnPublicEventPhase((uint)PublicEventPhase.ListenToDropShipBriefing);
        objective.Update(19);
        Assert.Equal(PublicEventStatus.Active, objective.Status);
        objective.Update(1);
        Assert.Equal(PublicEventStatus.Succeeded, objective.Status);
        Assert.Empty(f.EventProxy(418).GetInvocations(nameof(IPublicEvent.UpdateObjective)));
    }

    [Fact]
    public void VisibilityRemovalClearsNearbyMembershipOnceAndSupportsReentryAndTransfer()
    {
        var trigger = new Probe(1, Vector3.Zero);
        var player = new Probe(2, new Vector3(1, 0, 0));
        var map = RecordingDispatchProxy<IBaseMap>.Create(out var mp);
        mp.SetProperty(nameof(IBaseMap.Entry), new WorldEntry { Id = 1149 });
        typeof(GridEntity).GetProperty(nameof(GridEntity.Map))!.SetValue(player, map);
        trigger.SetInRangeCheck(10);
        trigger.AddVisible(player);
        player.AddVisible(trigger);
        Assert.Equal(1, trigger.Entered);
        player.OnRemoveFromMap();
        Assert.Equal(1, trigger.Exited);
        Assert.Empty(trigger.GetInRange<IGridEntity>(0));
        trigger.RemoveVisible(player);
        Assert.Equal(1, trigger.Exited);
        var transfer = new Probe(3, new Vector3(1, 0, 0));
        trigger.AddVisible(transfer);
        Assert.Equal(2, trigger.Entered);
        trigger.RemoveVisible(transfer);
        trigger.RemoveVisible(transfer);
        Assert.Equal(2, trigger.Exited);
    }

    [Fact]
    public void RemovingAnUnenteredVisibleEntityCannotCreateAPhantomRangeEntry()
    {
        var trigger = new Probe(1, Vector3.Zero);
        var player = new Probe(2, new Vector3(20, 0, 0));
        trigger.SetInRangeCheck(10);
        trigger.AddVisible(player);
        player.Move(Vector3.Zero);
        trigger.RemoveVisible(player);
        Assert.Equal(0, trigger.Entered);
        Assert.Equal(0, trigger.Exited);
    }

    [Theory]
    [InlineData(0u, 420u)]
    [InlineData(1u, 421u)]
    [InlineData(2u, 422u)]
    public void IntroSuccessStartsOneMappedVoteAndDefersOnlyChosenMissionUntilMapTick(uint winner, uint mission)
    {
        var f = new Fixture();
        Assert.Equal(new uint[] { 419, 418 }, f.Created);
        f.MapScript.OnPublicEventFinish(f.IntroEvent, f.Winner);
        f.MapScript.OnPublicEventFinish(f.IntroEvent, f.Winner);
        var vote = Assert.Single(f.MainProxy.GetInvocations(nameof(IPublicEvent.StartVote)));
        Assert.Equal(PublicEventTeam.PublicTeam, vote.Arguments[0]);
        Assert.Equal(45u, vote.Arguments[1]);
        Assert.Equal(0u, vote.Arguments[2]);
        f.Main.OnVoteFinished(45, winner);
        f.Main.OnVoteFinished(45, (winner + 1) % 3);
        Assert.Equal(new uint[] { 419, 418 }, f.Created);
        ((IUpdate)f.MapScript).Update(0.1);
        Assert.Equal(new uint[] { 419, 418, mission }, f.Created);
        Assert.Equal(2, f.EventProxy(mission).GetInvocations(nameof(IPublicEvent.JoinEvent)).Count);
        f.Main.OnAddToMap(f.Player(3));
        Assert.Equal(3, f.EventProxy(mission).GetInvocations(nameof(IPublicEvent.JoinEvent)).Count);
        f.MapScript.Update(10);
        Assert.Equal(3, f.Created.Count);
        f.MapScript.OnPublicEventFinish(f.MainEvent, f.Winner);
        Assert.Null(Assert.Single(f.EventProxy(mission).GetInvocations(nameof(IPublicEvent.Finish))).Arguments[0]);
    }

    [Fact]
    public void FailedIntroUnsolicitedVotesAndStoppedParentsCannotStartAMission()
    {
        var f = new Fixture();
        f.MapScript.OnPublicEventFinish(f.IntroEvent, null);
        f.Main.OnVoteFinished(45, 0);
        Assert.Empty(f.MainProxy.GetInvocations(nameof(IPublicEvent.StartVote)));
        f.MapScript.OnPublicEventFinish(f.IntroEvent, f.Winner);
        f.Main.OnVoteFinished(46, 0);
        f.Main.OnVoteFinished(45, 3);
        f.MapScript.Update(1);
        Assert.Equal(2, f.Created.Count);
        f.Main.OnVoteFinished(45, 1);
        f.Main.OnParentFinished();
        f.MapScript.Update(1);
        Assert.Equal(2, f.Created.Count);
    }

    [Fact]
    public void LateJoinNeverRejoinsTheFinishedIntroOrParent()
    {
        var f = new Fixture();
        f.EventProxy(418).SetProperty(nameof(IPublicEvent.IsFinalised), true);
        f.MapScript.OnAddToMap(f.Player(3));
        Assert.Empty(f.EventProxy(418).GetInvocations(nameof(IPublicEvent.JoinEvent)));
        Assert.Single(f.MainProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
        f.MainProxy.SetProperty(nameof(IPublicEvent.IsFinalised), true);
        f.MapScript.OnAddToMap(f.Player(4));
        Assert.Single(f.MainProxy.GetInvocations(nameof(IPublicEvent.JoinEvent)));
    }

    [Theory]
    [InlineData("missing-vote")]
    [InlineData("missing-child")]
    [InlineData("extra-choice")]
    [InlineData("wrong-label")]
    [InlineData("wrong-world")]
    [InlineData("wrong-parent")]
    public void UnsupportedOrMissingTablesDoNotOfferAnUnroutableVote(string scenario)
    {
        var f = new Fixture(scenario);
        f.Main.OnIntroComplete();
        Assert.Empty(f.MainProxy.GetInvocations(nameof(IPublicEvent.StartVote)));
    }

    [Fact]
    public void PartyChangesWaitForRemovalCleanupBeforeReducingTheWholePartyRequirement()
    {
        var f = new Fixture();
        var meet = MeetObjective(2);
        meet.UpdateObjective(1); // departing member was the only one inside
        f.SetMeetObjective(meet);
        f.Intro.OnPublicEventObjectiveStatus(Succeeded(2155));
        f.Intro.OnRemoveFromMap(f.Players[0]);
        f.Players.RemoveAt(0);
        Assert.Equal(2u, meet.DynamicMax);
        meet.UpdateObjective(-1); // range cleanup before the next map tick
        f.MapScript.Update(0.1);
        Assert.Equal(1u, meet.DynamicMax);
        Assert.Equal(0u, meet.Count);
        Assert.Equal(PublicEventStatus.Active, meet.Status);
        var joining = f.Player(3);
        f.Intro.OnAddToMap(joining);
        f.Players.Add(joining);
        f.MapScript.Update(0.1);
        Assert.Equal(2u, meet.DynamicMax);
        meet.UpdateObjective(1);
        Assert.Equal(PublicEventStatus.Active, meet.Status);
        meet.UpdateObjective(1);
        Assert.Equal(PublicEventStatus.Succeeded, meet.Status);
    }

    [Fact]
    public void DawsonActivationUsesParticipantTalkToCreditAndPreservesWorldBoundaries()
    {
        var f = new Fixture();
        IWorldEntity dawson = RecordingDispatchProxy<IWorldEntity>.Create(out var dp);
        dp.SetProperty(nameof(IWorldEntity.Map), f.Map);
        dp.SetProperty(nameof(IWorldEntity.CreatureId), 18365u);
        var script = new ViceMarshalDawsonEntityScript();
        script.OnLoad(dawson);
        script.OnActivateSuccess(null);
        script.OnActivateSuccess(RecordingDispatchProxy<IPlayer>.Create(out _));
        Assert.Empty(f.ManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        script.OnActivateSuccess(f.Players[0]);
        var call = Assert.Single(f.ManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Same(f.Players[0], call.Arguments[0]);
        Assert.Equal(PublicEventObjectiveType.TalkTo, call.Arguments[1]);
        Assert.Equal(7183u, call.Arguments[2]);
        dp.SetProperty(nameof(IWorldEntity.CreatureId), 17778u);
        script.OnActivateSuccess(f.Players[0]);
        Assert.Single(f.ManagerProxy.GetInvocations(nameof(IPublicEventManager.UpdateObjective)));
        Assert.Equal(18365u, Assert.Single(typeof(ViceMarshalDawsonEntityScript).GetCustomAttributes<ScriptFilterCreatureIdAttribute>()).CreatureId.Single());
    }

    private static NexusForever.Game.PublicEvent.PublicEventObjective MeetObjective(uint max)
    {
        var objective = PublicEventTestSupport.CreateObjective(new PublicEventObjectiveEntry
        { Id = 189, PublicEventId = 418, PublicEventTeamId = PublicEventTeam.PublicTeam,
            PublicEventObjectiveTypeEnum = PublicEventObjectiveType.ParticipantsInTriggerVolume, ObjectId = 1994, WorldLocation2Id = 13091, Count = 0 });
        objective.DynamicMax = max;
        objective.ActivateObjective();
        return objective;
    }

    private static IPublicEventObjective Succeeded(uint id)
    {
        var objective = RecordingDispatchProxy<IPublicEventObjective>.Create(out var op);
        op.SetProperty(nameof(IPublicEventObjective.Entry), new PublicEventObjectiveEntry { Id = id });
        op.SetProperty(nameof(IPublicEventObjective.Status), PublicEventStatus.Succeeded);
        return objective;
    }

    private sealed class Probe : GridEntity
    {
        public int Entered { get; private set; }
        public int Exited { get; private set; }
        public Probe(uint guid, Vector3 position) { Guid = guid; Position = position; }
        public void Move(Vector3 position) => Position = position;
        protected override void AddToRange(IGridEntity entity) { base.AddToRange(entity); Entered++; }
        protected override void RemoveFromRange(IGridEntity entity) { base.RemoveFromRange(entity); Exited++; }
    }

    private sealed class Fixture
    {
        public IContentMapInstance Map { get; }
        public TheHycrestInsurrectionMapScript MapScript { get; } = new();
        public TheHycrestInsurrectionEventScript Main { get; }
        public TheHycrestInsurrectionIntroEventScript Intro { get; } = new();
        public IPublicEvent MainEvent => events[419];
        public IPublicEvent IntroEvent => events[418];
        public RecordingDispatchProxy<IPublicEvent> MainProxy => EventProxy(419);
        public RecordingDispatchProxy<IPublicEventManager> ManagerProxy { get; }
        public List<IPlayer> Players { get; } = [];
        public List<uint> Created { get; } = [];
        public IPublicEventTeam Winner { get; }
        private readonly Dictionary<uint, IPublicEvent> events = [];

        public Fixture(string scenario = "valid")
        {
            Map = RecordingDispatchProxy<IContentMapInstance>.Create(out var mp);
            mp.SetProperty(nameof(IContentMapInstance.Entry), new WorldEntry { Id = 1149 });
            mp.SetMethodReturnFactory(nameof(IContentMapInstance.GetPlayers), () => Players);
            var manager = RecordingDispatchProxy<IPublicEventManager>.Create(out var mgr);
            ManagerProxy = mgr;
            mp.SetProperty(nameof(IContentMapInstance.PublicEventManager), manager);
            mgr.SetMethodHandler(nameof(IPublicEventManager.GetEvent), args => events.GetValueOrDefault((uint)args[0]));
            mgr.SetMethodHandler(nameof(IPublicEventManager.CreateEvent), args =>
            {
                uint id = (uint)args[0]; Created.Add(id);
                if (events.TryGetValue(id, out var existing)) return existing;
                var e = RecordingDispatchProxy<IPublicEvent>.Create(out var ep);
                ep.SetProperty(nameof(IPublicEvent.Id), id); ep.SetProperty(nameof(IPublicEvent.Map), Map);
                ep.SetProperty(nameof(IPublicEvent.ChildEventIds), id == 419 ? new uint[] {420,421,422,423,424,425} : Array.Empty<uint>());
                events[id] = e; return e;
            });
            MapScript.OnLoad(Map);
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
            var vote = new PublicEventVoteEntry { Id = 45, DurationMS = 45000, LocalizedTextIdOption = [432875,432876,432877,0,0], LocalizedTextIdLabel = [432878,432879,432880,0,0] };
            if (scenario == "extra-choice") vote.LocalizedTextIdOption[3] = 1;
            if (scenario == "wrong-label") vote.LocalizedTextIdLabel[0] = 1;
            tp.SetProperty(nameof(IGameTableManager.PublicEventVote), PlayerBindPointTests.Table<PublicEventVoteEntry>(scenario == "missing-vote" ? [] : [vote]));
            var children = new[] {420u,421u,422u}.Where(id => scenario != "missing-child" || id != 422).Select(id => new PublicEventEntry
            { Id = id, WorldId = scenario == "wrong-world" ? 1u : 1149u, PublicEventIdParent = scenario == "wrong-parent" ? 1u : 419u }).ToArray();
            tp.SetProperty(nameof(IGameTableManager.PublicEvent), PlayerBindPointTests.Table(children));
            Main = new(tables); Main.OnLoad(MainEvent); Intro.OnLoad(IntroEvent);
            foreach (var e in events.Values) ((RecordingDispatchProxy<IPublicEvent>)(object)e).SetMethodHandler(nameof(IPublicEvent.InvokeScriptCollection), (method,args) =>
            {
                if (method.GetGenericArguments()[0] == typeof(TheHycrestInsurrectionEventScript)) ((Action<TheHycrestInsurrectionEventScript>)args[0])(Main);
                else if (method.GetGenericArguments()[0] == typeof(TheHycrestInsurrectionIntroEventScript)) ((Action<TheHycrestInsurrectionIntroEventScript>)args[0])(Intro);
                return null;
            });
            Winner = RecordingDispatchProxy<IPublicEventTeam>.Create(out var wp);
            wp.SetProperty(nameof(IPublicEventTeam.Team), PublicEventTeam.PublicTeam);
            Players.Add(Player(1)); Players.Add(Player(2));
        }

        public IPlayer Player(ulong id)
        {
            var p = RecordingDispatchProxy<IPlayer>.Create(out var pp);
            pp.SetProperty(nameof(IPlayer.CharacterId), id); pp.SetProperty(nameof(IPlayer.Guid), (uint)id); pp.SetProperty(nameof(IPlayer.Map), Map); return p;
        }
        public RecordingDispatchProxy<IPublicEvent> EventProxy(uint id) => (RecordingDispatchProxy<IPublicEvent>)(object)events[id];
        public void SetMeetObjective(IPublicEventObjective objective)
        {
            var team = RecordingDispatchProxy<IPublicEventTeam>.Create(out var tp);
            tp.SetMethodReturn(nameof(IPublicEventTeam.GetObjectives), new[] { objective });
            EventProxy(418).SetMethodReturn(nameof(IPublicEvent.GetTeams), new[] { team });
        }
    }
}
