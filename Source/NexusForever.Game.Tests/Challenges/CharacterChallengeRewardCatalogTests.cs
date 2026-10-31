using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Challenges;

public sealed class CharacterChallengeRewardCatalogTests
{
    [Fact]
    public void ActualBuild16042_AllSixteenCatalogsAnd159Rows475ChoicesArePreserved()
    {
        var fixture = new Fixture();
        CharacterChallengeRewardCatalog catalog = fixture.Build();
        Assert.Equal(32, fixture.Tracks.Count);
        Assert.Equal(446, fixture.Rewards.Count);
        Assert.Equal(Enumerable.Range(15, 14).Select(id => (uint)id).Concat(new uint[] { 33, 36 }), catalog.Tracks.Select(track => track.Id));
        Assert.Equal(new uint[] { 29, 30, 31, 32, 34 }, catalog.UnsupportedTrackIds);
        CharacterChallengeRewardDefinition[] rows = catalog.Tracks.SelectMany(track => track.Slots).SelectMany(slot => slot.Candidates).ToArray();
        Assert.Equal(159, rows.Length);
        Assert.Equal(475, rows.Sum(row => row.Choices.Count));
        Assert.All(rows, row =>
        {
            Assert.True(row.ForceSoulbind);
            Assert.False(row.AutoClaim);
            Assert.InRange(row.Choices.Count, 1, 3);
            RewardTrackRewardsEntry actual = fixture.Reward(row.Id);
            uint[] actualIds = [actual.RewardChoiceId00, actual.RewardChoiceId01, actual.RewardChoiceId02];
            uint[] actualCounts = [actual.RewardChoiceCount00, actual.RewardChoiceCount01, actual.RewardChoiceCount02];
            Assert.All(row.Choices, choice =>
            {
                Assert.Equal(actualIds[choice.Index], choice.ItemId);
                Assert.Equal(actualCounts[choice.Index], choice.Count);
                Assert.NotEqual(0u, choice.Count);
            });
            Assert.Null(row.GetChoice(uint.MaxValue));
        });
        Assert.Equal(158, rows.Count(row => row.Choices.Count == 3));
        Assert.Single(rows, row => row.Choices.Count == 1);
    }

    [Theory]
    [InlineData(15u, "315,316,317,319")]
    [InlineData(16u, "320,321,324")]
    [InlineData(33u, "385,386,387,389")]
    [InlineData(36u, "536,538,539,537")]
    public void ActualSingleCandidateCatalogsPreserveFlagOrdinalIncludingArcterra(uint trackId, string expected)
    {
        var fixture = new Fixture();
        fixture.Tracks.Reverse(); fixture.Rewards.Reverse();
        var catalog = fixture.Build();
        var selection = catalog.CreateSelection(trackId, 19, count => { Assert.Equal(1, count); return 0; });
        Assert.Equal(expected.Split(',').Select(uint.Parse), selection.Slots.Select(slot => slot.Reward.Id));
        Assert.Equal(19ul, selection.CycleId);
        for (int i = 0; i < selection.Slots.Count; i++)
        {
            var slot = selection.Slots[i];
            Assert.Equal(i, slot.Position);
            Assert.Equal(i == selection.Slots.Count - 1, slot.Reward.IsCasque);
            Assert.True(catalog.TryGetReward(slot.Reward.Id, out var reference));
            Assert.Equal(i, reference.Slot.Position);
            Assert.Same(slot.Reward, reference.Reward);
            Assert.Same(catalog.GetTrack(trackId), reference.Track);
        }
    }

    [Fact]
    public void ActualFarsidePoolSamplesExactlyOncePerBucketWithInjectedBounds()
    {
        var catalog = new Fixture().Build();
        var bounds = new List<int>();
        var selection = catalog.CreateSelection(24, 0, count => { bounds.Add(count); return count - 1; });
        Assert.Equal(new[] { 4, 4, 3, 1 }, bounds);
        Assert.Equal(new uint[] { 478, 482, 485, 486 }, selection.Slots.Select(slot => slot.Reward.Id));
        Assert.Equal(new uint[] { 300, 600, 900, 1200 }, selection.Slots.Select(slot => slot.Cost));
        Assert.Equal(new byte[] { 0, 1, 2, 3 }, selection.Slots.Select(slot => slot.Position));
    }

    [Fact]
    public void PersistedSelectionRestoresWithoutResamplingAndRejectsIdSorting()
    {
        var catalog = new Fixture().Build();
        var original = catalog.CreateSelection(24, 11, count => count - 1);
        var restored = catalog.RestoreSelection(24, 11, original.Slots.Select(slot => slot.Reward.Id));
        Assert.Equal(original.Slots, restored.Slots);
        Assert.Throws<InvalidDataException>(() => catalog.RestoreSelection(36, 0, new uint[] { 536, 537, 538, 539 }));
        Assert.Throws<InvalidDataException>(() => catalog.RestoreSelection(24, 0, new uint[] { 475, 479, 483 }));
        Assert.Throws<InvalidDataException>(() => catalog.RestoreSelection(24, 0, new uint[] { 475, 479, 483, 0 }));
        Assert.Throws<InvalidDataException>(() => catalog.RestoreSelection(24, 0, new uint[] { 475, 479, 483, 486, 486 }));
    }

    [Theory]
    [InlineData(29u)]
    [InlineData(30u)]
    [InlineData(31u)]
    [InlineData(32u)]
    [InlineData(34u)]
    public void EmptyActualCatalogIsExplicitlyUnsupportedWithoutBreakingOtherOwners(uint id)
    {
        var catalog = new Fixture().Build();
        Assert.Null(catalog.GetTrack(id));
        Assert.False(catalog.TryGetTrack(id, out _));
        Assert.Throws<NotSupportedException>(() => catalog.CreateSelection(id, 0, _ => throw new Exception("must not sample")));
        Assert.NotNull(catalog.GetTrack(16));
        Assert.Null(catalog.GetTrack(9));
        Assert.False(catalog.TryGetReward(276, out _));
        Assert.Throws<KeyNotFoundException>(() => catalog.CreateSelection(9, 0));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void InvalidSelectorIndexIsRejected(int index)
        => Assert.Throws<ArgumentOutOfRangeException>(() => new Fixture().Build().CreateSelection(24, 0, _ => index));

    [Fact]
    public void ProductionCryptoSelectorAlwaysSelectsAnEligibleRowWithoutChangingStaticData()
    {
        var catalog = new Fixture().Build();
        var selection = catalog.CreateSelection(24, 3);
        Assert.All(selection.Slots, selected =>
            Assert.Contains(catalog.GetTrack(24).Slots[selected.Position].Candidates, candidate => candidate.Id == selected.Reward.Id));
    }

    [Fact]
    public void CatalogAndSelectionAreDetachedFromMutableTableEntriesAndCollectionsAreReadOnly()
    {
        var fixture = new Fixture();
        var catalog = fixture.Build();
        var selection = catalog.CreateSelection(16, 0, _ => 0);
        fixture.Track(16).RewardPointCost00 = 999;
        fixture.Reward(320).RewardChoiceId00 = 999;
        fixture.Reward(320).Flags = 0;
        Assert.Equal(100u, selection.Slots[0].Cost);
        Assert.NotEqual(999u, selection.Slots[0].Reward.Choices[0].ItemId);
        Assert.True(selection.Slots[0].Reward.ForceSoulbind);
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterChallengeTrackDefinition>)catalog.Tracks).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterChallengeRewardSlot>)catalog.GetTrack(16).Slots).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterChallengeRewardDefinition>)catalog.GetTrack(16).Slots[0].Candidates).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterChallengeRewardChoice>)selection.Slots[0].Reward.Choices).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<CharacterChallengeSelectedSlot>)selection.Slots).Clear());
    }

    [Fact]
    public void EqualCumulativeCostsRemainSeparateClaimOrdinals()
    {
        var fixture = new Fixture();
        fixture.Track(16).RewardPointCost01 = 100;
        var selection = fixture.Build().CreateSelection(16, 0, _ => 0);
        Assert.Equal(new uint[] { 100, 100, 300 }, selection.Slots.Select(slot => slot.Cost));
        Assert.Equal(new uint[] { 320, 321, 324 }, selection.Slots.Select(slot => slot.Reward.Id));
        Assert.Equal(new byte[] { 0, 1, 2 }, selection.Slots.Select(slot => slot.Position));
    }

    [Theory]
    [InlineData("missing-track-table")]
    [InlineData("missing-reward-table")]
    [InlineData("duplicate-track")]
    [InlineData("duplicate-reward")]
    [InlineData("track-wire-overflow")]
    [InlineData("reward-wire-overflow")]
    [InlineData("zero-track")]
    [InlineData("zero-reward")]
    [InlineData("parent")]
    [InlineData("track-flags")]
    [InlineData("cost-gap")]
    [InlineData("cost-decreasing")]
    [InlineData("no-cost")]
    [InlineData("missing-position")]
    [InlineData("zero-mask")]
    [InlineData("multibit-mask")]
    [InlineData("overflow-mask")]
    [InlineData("early-casque")]
    [InlineData("missing-final-casque")]
    [InlineData("unbound")]
    [InlineData("autoclaim")]
    [InlineData("prerequisite")]
    [InlineData("currency-type")]
    [InlineData("currency-amount")]
    [InlineData("choice-type")]
    [InlineData("choice-id")]
    [InlineData("choice-zero-count")]
    [InlineData("choice-gap")]
    [InlineData("no-choice")]
    public void MalformedNonemptyCatalogFailsClosed(string mutation)
    {
        var f = new Fixture();
        RewardTrackEntry track = f.Track(16);
        RewardTrackRewardsEntry reward = f.Reward(320);
        switch (mutation)
        {
            case "missing-track-table": break;
            case "missing-reward-table": break;
            case "duplicate-track": f.Tracks.Add(track); break;
            case "duplicate-reward": f.Rewards.Add(reward); break;
            case "track-wire-overflow": track.Id = 0x4000; break;
            case "reward-wire-overflow": reward.Id = 0x4000; break;
            case "zero-track": track.Id = 0; break;
            case "zero-reward": reward.Id = 0; break;
            case "parent": track.RewardTrackIdParent = 15; break;
            case "track-flags": track.Flags = 1; break;
            case "cost-gap": track.RewardPointCost01 = 0; break;
            case "cost-decreasing": track.RewardPointCost01 = 99; break;
            case "no-cost": track.RewardPointCost00 = track.RewardPointCost01 = track.RewardPointCost02 = 0; break;
            case "missing-position": f.Rewards.Remove(f.Reward(321)); break;
            case "zero-mask": reward.RewardPointFlags = 0; break;
            case "multibit-mask": reward.RewardPointFlags = 3; break;
            case "overflow-mask": reward.RewardPointFlags = 8; break;
            case "early-casque": reward.Flags = 3; break;
            case "missing-final-casque": f.Reward(324).Flags = 2; break;
            case "unbound": reward.Flags = 0; break;
            case "autoclaim": reward.Flags = 6; break;
            case "prerequisite": reward.PrerequisiteId = 1; break;
            case "currency-type": reward.CurrencyTypeId = 1; break;
            case "currency-amount": reward.CurrencyAmount = 1; break;
            case "choice-type": reward.RewardTrackRewardTypeEnum00 = 1; break;
            case "choice-id": reward.RewardChoiceId00 = 0; break;
            case "choice-zero-count": reward.RewardChoiceCount00 = 0; break;
            case "choice-gap": reward.RewardChoiceId02 = 9; reward.RewardChoiceCount02 = 1; break;
            case "no-choice": reward.RewardChoiceId00 = reward.RewardChoiceCount00 = 0; break;
        }
        Assert.Throws<InvalidDataException>(() => f.Build(mutation));
    }

    [Fact]
    public void OtherOwnersUnsupportedRowsDoNotAlterChallengeCatalog()
    {
        var f = new Fixture();
        f.Reward(276).RewardTrackRewardTypeEnum00 = uint.MaxValue;
        Assert.Equal(16, f.Build().Tracks.Count);
    }

    [Fact]
    public void AcceptedPolicyFinalClaimFirstDoesNotResetOrConsumeRandomness()
    {
        var catalog = new Fixture().Build();
        var current = catalog.CreateSelection(24, 8, _ => 0);
        bool advanced = catalog.TryAdvanceCycle(current, 1500, 8, out var next, out ulong carry,
            _ => throw new Exception("must not sample before all claims"));
        Assert.False(advanced);
        Assert.Null(next);
        Assert.Equal(1500ul, carry);
        Assert.Equal(8ul, current.CycleId);
    }

    [Fact]
    public void AcceptedPolicyAllClaimsAdvanceOnceCarryExcessAndKeepNewBlueprint()
    {
        var catalog = new Fixture().Build();
        var current = catalog.CreateSelection(24, 8, _ => 0);
        int calls = 0;
        Assert.True(catalog.TryAdvanceCycle(current, 2700, 15, out var next, out ulong carry,
            count => { calls++; return count - 1; }));
        Assert.Equal(4, calls);
        Assert.Equal(9ul, next.CycleId);
        Assert.Equal(1500ul, carry); // Exactly one reset; excess can exceed the next maximum.
        Assert.Equal(new uint[] { 478, 482, 485, 486 }, next.Slots.Select(slot => slot.Reward.Id));
        Assert.Equal(new uint[] { 475, 479, 483, 486 }, current.Slots.Select(slot => slot.Reward.Id));
        Assert.False(catalog.TryAdvanceCycle(next, carry, 0, out _, out _, _ => throw new Exception("new cycle remains unclaimed")));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(8, 3)]
    [InlineData(0, 33)]
    [InlineData(0, 0)]
    public void InvalidClaimMaskOrSlotCountIsRejected(uint mask, int count)
    {
        if (mask == 0 && count == 1) Assert.False(CharacterChallengeCyclePolicy.AllClaimed(mask, count));
        else Assert.Throws<ArgumentOutOfRangeException>(() => CharacterChallengeCyclePolicy.AllClaimed(mask, count));
    }

    [Fact]
    public void All32ClaimBitsAreHandledWithoutShiftWrap()
        => Assert.True(CharacterChallengeCyclePolicy.AllClaimed(uint.MaxValue, 32));

    [Fact]
    public void InvalidTransitionBoundsRejectBeforeSampling()
    {
        var catalog = new Fixture().Build();
        var current = catalog.CreateSelection(16, 0, _ => 0);
        int calls = 0;
        int Selector(int count) { calls++; return 0; }
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.TryAdvanceCycle(current, (ulong)uint.MaxValue + 1, 7, out _, out _, Selector));
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.TryAdvanceCycle(current, 300, 8, out _, out _, Selector));
        Assert.Throws<InvalidDataException>(() => catalog.TryAdvanceCycle(current, 299, 7, out _, out _, Selector));
        var last = catalog.RestoreSelection(16, ulong.MaxValue, current.Slots.Select(slot => slot.Reward.Id));
        Assert.Throws<OverflowException>(() => catalog.TryAdvanceCycle(last, 300, 7, out _, out _, Selector));
        Assert.Equal(0, calls);
    }

    private sealed class Fixture
    {
        public List<RewardTrackEntry> Tracks { get; } = LoyaltyCatalogBuild16042Fixture.Tracks().ToList();
        public List<RewardTrackRewardsEntry> Rewards { get; } = LoyaltyCatalogBuild16042Fixture.Rewards().ToList();
        public RewardTrackEntry Track(uint id) => Tracks.Single(row => row.Id == id);
        public RewardTrackRewardsEntry Reward(uint id) => Rewards.Single(row => row.Id == id);
        public CharacterChallengeRewardCatalog Build(string mutation = null)
        {
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
            if (mutation != "missing-track-table") proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(Tracks.ToArray()));
            if (mutation != "missing-reward-table") proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(Rewards.ToArray()));
            return new CharacterChallengeRewardCatalog(tables);
        }
    }
}
