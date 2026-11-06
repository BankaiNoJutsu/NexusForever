using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Static.Reputation;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public class CommunicatorSchedulingTests
{
    [Theory]
    [InlineData(-1f, true)] [InlineData(0f, true)] [InlineData(10f, true)] [InlineData(float.NaN, false)]
    public void ReputationZeroBoundsDoNotImposeAFiniteFloor(float amount, bool eligible)
    {
        var row = Row(1, 0); row.Quests = []; row.States = []; row.FactionIdReputation = 1;
        var view = new View { Reputation = amount };
        Assert.Equal(eligible ? CommunicatorResult.Eligible : CommunicatorResult.Reputation, Index(row).Messages[1].Evaluate(view));
    }
    [Theory]
    [InlineData(0u)] [InlineData(1u)] [InlineData(5u)] [InlineData(10u)] [InlineData(11u)]
    [InlineData(12u)] [InlineData(13u)] [InlineData(14u)] [InlineData(19u)] [InlineData(20u)]
    public void MissingQuestMetadataFailsEveryCondition(uint state)
        => Assert.False(CommunicatorConditions.Meets(state, null, new(QuestState.Accepted, true, 255)));

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void ObjectivePairsReadOnlyTheSpecifiedBitOnAnActiveRecord(byte index)
    {
        var info = new CommunicatorQuestInfo(6, false, 0);
        var complete = new CommunicatorQuestSnapshot(QuestState.Accepted, true, (byte)(1 << index));
        var incomplete = complete with { CompletedObjectives = (byte)(255 ^ (1 << index)) };
        Assert.True(CommunicatorConditions.Meets(5u + index, info, complete));
        Assert.False(CommunicatorConditions.Meets(5u + index, info, incomplete));
        Assert.True(CommunicatorConditions.Meets(14u + index, info, incomplete));
        Assert.False(CommunicatorConditions.Meets(14u + index, info, complete));
        Assert.False(CommunicatorConditions.Meets(5u + index, info, complete with { Native = false }));
        Assert.False(CommunicatorConditions.Meets(14u + index, info, incomplete with { Native = false }));
    }

    [Fact]
    public void RepeatableStateElevenDoesNotMakeAnExistingRecordANewOffer()
    {
        Assert.True(CommunicatorConditions.Meets(11, new(0, true, 4), new(QuestState.Completed, false, 0)));
        Assert.False(CommunicatorConditions.Meets(11, new(0, false, 4), new(QuestState.Completed, false, 0)));
        Assert.Equal(CommunicatorOfferBlock.RecordExists, CommunicatorConditions.Offer(QuestState.Completed, true, 4));
        Assert.Equal(CommunicatorOfferBlock.NoCallbackFlag, CommunicatorConditions.Offer(null, true, 0x10000));
        Assert.True(CommunicatorConditions.Meets(19, new(1, false, 0), new(QuestState.Accepted, true, 0)));
    }

    [Theory]
    [InlineData(1u, false, true, 0u, 0u, 0u, CommunicatorKind.PushedScheduled)]
    [InlineData(0u, false, true, 200u, 0u, 0u, CommunicatorKind.ClientScheduled)]
    [InlineData(1u, true, true, 200u, 0u, 0u, CommunicatorKind.ChainTarget)]
    [InlineData(1u, false, false, 200u, 2u, 0u, CommunicatorKind.WorldArmed)]
    [InlineData(1u, false, false, 200u, 0u, 5u, CommunicatorKind.WorldArmed)]
    [InlineData(1u, false, false, 200u, 0u, 0u, CommunicatorKind.PushedUnconditioned)]
    [InlineData(1u, false, false, 0u, 2u, 0u, CommunicatorKind.PushedScript)]
    [InlineData(0u, false, false, 200u, 0u, 0u, CommunicatorKind.ClientUnscheduled)]
    [InlineData(0u, false, true, 0u, 0u, 0u, CommunicatorKind.ClientOnly)]
    public void ClassificationSeparatesEventWorldClientAndScriptOwnership(uint flags, bool chain, bool trigger, uint delivered, uint zone, uint level, CommunicatorKind expected)
        => Assert.Equal(expected, CommunicatorIndex.Classify(new() { Flags = flags, QuestIdDelivered = delivered, WorldZoneId = zone, MinLevel = level }, chain, trigger));

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void StateDeliveryWaitsForDrainAndMentionsOnceThroughReentrancy(bool pushed)
    {
        var view = new View(); view.Quests[1] = new(QuestState.Accepted, true, 0);
        var index = Index(Row(10, 1, 200, pushed ? 1u : 0u)); var seen = new List<uint>();
        CommunicatorTracker tracker = null;
        tracker = new(view, index, (message, _) =>
        {
            seen.Add(message.Id); Assert.Equal(pushed, message.ServerPushed);
            view.Quests[200] = new(QuestState.Mentioned, false, 0);
            tracker.OnQuestState(200, QuestState.Accepted, QuestState.Mentioned, true);
        });
        tracker.OnQuestState(1, QuestState.Accepted, QuestState.Accepted, true);
        Assert.Empty(seen); tracker.Update(0); tracker.Update(1);
        Assert.Equal([10u], seen);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void PendingConditionsRetryUntilTheTriggerChangesOrTheDeadlineExpires(bool expire)
    {
        var view = new View(); view.Quests[1] = new(QuestState.Accepted, true, 0);
        var row = Row(10, 1); row.MinLevel = 5; int sent = 0;
        var tracker = new CommunicatorTracker(view, Index(row), (_, _) => ++sent);
        tracker.OnQuestState(1, QuestState.Unknown, QuestState.Accepted); tracker.Update(0);
        Assert.Equal(1, tracker.PendingCount); tracker.Update(0.9); Assert.Equal(0, sent);
        if (expire) tracker.Update(300); else view.Quests[1] = new(QuestState.Completed, false, 0);
        view.Level = 5; tracker.Update(1);
        Assert.Equal(0, sent); Assert.Equal(0, tracker.PendingCount);
    }

    [Fact]
    public void PendingLevelGateCanSucceedAndObjectiveEventsAreNotStateFive()
    {
        var view = new View(); view.Quests[1] = new(QuestState.Accepted, true, 1);
        var row = Row(10, 5); row.MinLevel = 5; int sent = 0;
        var tracker = new CommunicatorTracker(view, Index(row), (_, _) => ++sent);
        tracker.OnQuestState(1, QuestState.Unknown, QuestState.Mentioned); tracker.Update(0); Assert.Equal(0, sent);
        tracker.OnObjectiveComplete(1, 0); tracker.Update(0); Assert.Equal(1, tracker.PendingCount);
        view.Level = 5; tracker.Update(1); Assert.Equal(1, sent); Assert.Equal(0, tracker.PendingCount);
    }

    [Fact]
    public void WorldRowsFireOnZoneAndCrossedLevelsButDoNotBackfillLevelsAtLogin()
    {
        var zone = Row(10, 0, 200); zone.Quests = []; zone.States = []; zone.WorldZoneId = 2;
        var level = Row(11, 0, 201); level.Quests = []; level.States = []; level.MinLevel = 5;
        var view = new View { Level = 10, Zone = 2 }; var seen = new List<uint>();
        var tracker = new CommunicatorTracker(view, Index(zone, level), (m, _) => { seen.Add(m.Id); view.Quests[m.QuestId] = new(QuestState.Mentioned, false, 0); });
        tracker.OnEnteredWorld(); tracker.Update(0); Assert.Equal([10u], seen);
        tracker.OnZone(2); tracker.Update(0); Assert.Equal([10u], seen);
        tracker.OnLevel(4, 5); tracker.Update(0); Assert.Equal([10u, 11u], seen);
    }

    [Fact]
    public void IndexPreservesMissingDataFailureAndDoesNotArmScriptOrChainedRows()
    {
        var head = Row(10, 1); head.CommunicatorMessagesIdNext = 11;
        var next = Row(11, 1); var missing = Row(12, 1); missing.Quests = [99];
        var broken = Row(13, 1); broken.States = [];
        var index = Index(head, next, missing, broken);
        Assert.Equal([10u], index.GetTriggers(1).Select(a => a.Message.Id));
        Assert.False(index.Messages[11].Reachable);
        Assert.Equal(CommunicatorResult.QuestCondition, index.Messages[12].Evaluate(new View()));
        Assert.Equal(CommunicatorResult.QuestCondition, index.Messages[13].Evaluate(new View()));
    }

    internal static CommunicatorMessagesEntry Row(uint id, uint state, uint delivered = 0, uint flags = 1)
        => new() { Id = id, Quests = [1], States = [state], QuestIdDelivered = delivered, Flags = flags };
    internal static CommunicatorIndex Index(params CommunicatorMessagesEntry[] rows)
        => CommunicatorIndex.Build(rows, id => id is 1 or 200 or 201 ? new CommunicatorQuestInfo(6, false, 4) : null);
    internal sealed class View : ICommunicatorPlayerView
    {
        public readonly Dictionary<ushort, CommunicatorQuestSnapshot> Quests = [];
        public uint WorldId => 1;
        public Class Class => 0;
        public Race Race => 0;
        public Faction Faction => 0;
        public uint Level { get; set; } = 1;
        public uint Zone { get; set; }
        public bool IsInZone(uint id) => Zone == id;
        public float Reputation { get; set; }
        public float GetReputation(Faction faction) => Reputation;
        public CommunicatorQuestSnapshot? GetQuest(ushort id) => Quests.TryGetValue(id, out var row) ? row : null;
        public bool MeetsPrerequisite(uint id) => true;
        public CommunicatorOfferBlock CheckOffer(ushort id) => Quests.ContainsKey(id) ? CommunicatorOfferBlock.RecordExists : CommunicatorOfferBlock.None;
    }
}
