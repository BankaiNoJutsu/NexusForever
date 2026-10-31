using System.Reflection;
using System.Text.Json;
using NexusForever.Game.Account.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Account.Reward;

public class LoyaltyCatalogTests
{
    [Fact]
    public void FullActualCatalogPreservesAllEightTiersAndFortyFourRewards()
    {
        var f = new Fixture();
        Assert.Equal(32, f.Tracks.Count);
        Assert.Equal(446, f.Rewards.Count);
        var catalog = f.Build();
        Assert.Equal(new uint[] { 9, 10, 11, 12, 13, 14, 38, 39 }, catalog.Tracks.Select(t => t.Id));
        Assert.Equal(new uint[] { 0, 9, 10, 11, 12, 13, 14, 38 }, catalog.Tracks.Select(t => t.ParentId));
        Assert.Equal(new ulong[] { 0, 4000, 9000, 24000, 80000, 150000, 300000, 600000 }, catalog.Tracks.Select(t => t.Offset));
        Assert.Equal(44, catalog.Tracks.Sum(t => t.Slots.Count));
        Assert.Equal(1_200_000ul, catalog.Tracks[^1].Offset + catalog.Tracks[^1].MaximumPoints);
        Assert.All(catalog.Tracks.SelectMany(t => t.Slots), slot =>
        {
            Assert.True(slot.AutoClaim);
            Assert.True(slot.ForceSoulbind);
            Assert.Equal(1u << slot.Position, slot.Entry.RewardPointFlags);
            Assert.Contains(f.Items, item => item.Id == slot.AccountItemId);
        });
        Assert.Null(catalog.GetTrack(4));
        Assert.Null(catalog.GetTrack(35));
        Assert.Null(catalog.GetReward(f.Rewards.First(r => r.RewardTrackId == 24).Id));
        Assert.Null(catalog.GetReward(uint.MaxValue));
    }

    [Theory]
    [InlineData(9u, "276,312,278,280,311,279")]
    [InlineData(10u, "282,314,283,284,390,285")]
    [InlineData(11u, "288,287,290,289,281,291")]
    [InlineData(12u, "293,294,295,296,297")]
    [InlineData(13u, "298,299,300,302,301")]
    [InlineData(14u, "304,305,306,307,308,309")]
    [InlineData(38u, "548,549,550,551,552")]
    [InlineData(39u, "554,555,556,557,558")]
    public void CanonicalPositionUsesNativeFlagsRatherThanRowIdOrTableOrder(uint id, string expected)
    {
        var f = new Fixture();
        f.Tracks.Reverse();
        f.Rewards.Reverse();
        var catalog = f.Build();
        var track = catalog.GetTrack(id);
        Assert.Equal(expected.Split(',').Select(uint.Parse), track.Slots.Select(s => s.Entry.Id));
        for (byte position = 0; position < track.Slots.Count; position++)
        {
            var slot = track.Slots[position];
            Assert.Equal(position, slot.Position);
            var reference = catalog.GetReward(slot.Entry.Id);
            Assert.Same(track, reference.Track);
            Assert.Same(slot, reference.Slot);
            Assert.Equal(position == track.Slots.Count - 1, (slot.Entry.Flags & 1u) != 0);
        }
    }

    [Fact]
    public void SparseAndPermutedTrackNineWitnessCannotShiftCostsOrClaimPositions()
    {
        var catalog = new Fixture().Build();
        var track = catalog.GetTrack(9);
        Assert.Equal(new uint[] { 800, 1600, 2400, 3200, 3200, 4000 }, track.Slots.Select(s => s.Cost));
        Assert.Equal(new uint[] { 276, 312 }, track.Slots.Where(s => s.Cost <= 1600).Select(s => s.Entry.Id));
        Assert.Equal(2, catalog.GetReward(278).Slot.Position);
        Assert.Equal(2400u, catalog.GetReward(278).Slot.Cost);
        Assert.Equal(5, catalog.GetReward(279).Slot.Position);
        Assert.Equal(4000u, catalog.GetReward(279).Slot.Cost);
        Assert.Equal(3200u, catalog.GetReward(280).Slot.Cost);
        Assert.Equal(3200u, catalog.GetReward(311).Slot.Cost);
        Assert.NotEqual(catalog.GetReward(280).Slot.Position, catalog.GetReward(311).Slot.Position);
        var tier2 = catalog.GetTrack(10);
        Assert.Equal(1000u, tier2.Slots[0].Cost);
        Assert.Equal(1000u, tier2.Slots[1].Cost);
        Assert.NotEqual(tier2.Slots[0].Entry.Id, tier2.Slots[1].Entry.Id);
    }

    [Fact]
    public void CatalogReadsStaticDataWithoutMutatingEntriesOrNonLoyaltyPolicy()
    {
        var f = new Fixture();
        // Unsupported non-Loyalty choices/costs remain outside this catalog.
        f.Tracks.Single(t => t.Id == 4).RewardPointCost00 = 0;
        f.Rewards.First(r => r.RewardTrackId == 24).RewardTrackRewardTypeEnum00 = 999;
        var options = new JsonSerializerOptions { IncludeFields = true };
        string before = JsonSerializer.Serialize(new { f.Tracks, f.Rewards, f.Items }, options);
        var catalog = f.Build();
        Assert.Equal(before, JsonSerializer.Serialize(new { f.Tracks, f.Rewards, f.Items }, options));
        Assert.Equal(8, catalog.Tracks.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<LoyaltyTrackDefinition>)catalog.Tracks)[0] = catalog.Tracks[0]);
        Assert.Throws<NotSupportedException>(() => ((IList<LoyaltyRewardSlot>)catalog.Tracks[0].Slots)[0] = catalog.Tracks[0].Slots[0]);
    }

    [Theory]
    [InlineData(0u, false, false)]
    [InlineData(2u, false, true)]
    [InlineData(4u, true, false)]
    [InlineData(6u, true, true)]
    public void MappedRewardFlagsRemainIndependent(uint flags, bool autoClaim, bool soulbind)
    {
        var f = new Fixture();
        f.Reward(276).Flags = flags;
        var slot = f.Build().GetReward(276).Slot;
        Assert.Equal(autoClaim, slot.AutoClaim);
        Assert.Equal(soulbind, slot.ForceSoulbind);
    }

    [Fact]
    public void OffsetsRemainUnsigned64BitAndUseMaximumRatherThanSumOfMilestones()
    {
        var f = new Fixture();
        f.Track(9).RewardPointCost05 = uint.MaxValue;
        f.Track(10).RewardPointCost05 = uint.MaxValue;
        var catalog = f.Build();
        Assert.Equal((ulong)uint.MaxValue, catalog.GetTrack(10).Offset);
        Assert.Equal(2ul * uint.MaxValue, catalog.GetTrack(11).Offset);
    }

    [Theory]
    [InlineData("root-missing")]
    [InlineData("root-parent")]
    [InlineData("root-type")]
    [InlineData("duplicate-track")]
    [InlineData("track-wire-overflow")]
    [InlineData("track-identity-collision")]
    [InlineData("nonlinear")]
    [InlineData("track-flags")]
    [InlineData("orphan")]
    [InlineData("branch")]
    [InlineData("extra-root")]
    [InlineData("disconnected-cycle")]
    [InlineData("empty-costs")]
    [InlineData("cost-hole")]
    [InlineData("decreasing-cost")]
    [InlineData("extra-cost")]
    [InlineData("missing-slot")]
    [InlineData("extra-slot")]
    [InlineData("zero-mask")]
    [InlineData("multiple-mask-bits")]
    [InlineData("duplicate-reward-id")]
    [InlineData("reward-wire-overflow")]
    [InlineData("ambiguous-slot")]
    [InlineData("nonfinal-casque")]
    [InlineData("final-not-casque")]
    [InlineData("reward-flags")]
    [InlineData("reward-currency-type")]
    [InlineData("reward-currency-amount")]
    [InlineData("item2-choice")]
    [InlineData("missing-choice")]
    [InlineData("zero-count")]
    [InlineData("multiple-count")]
    [InlineData("second-choice")]
    [InlineData("empty-second-type")]
    [InlineData("empty-second-count")]
    [InlineData("third-choice")]
    [InlineData("missing-account-item")]
    public void MalformedCatalogFailsClosedBeforeAnyMilestoneCanShift(string scenario)
    {
        var f = new Fixture();
        switch (scenario)
        {
            case "root-missing": f.Tracks.Remove(f.Track(9)); break;
            case "root-parent": f.Track(9).RewardTrackIdParent = 39; break;
            case "root-type": f.Track(9).RewardTrackTypeEnum = 1; break;
            case "duplicate-track": f.Tracks.Add(f.Track(9)); break;
            case "track-wire-overflow": f.Track(10).Id = 1u << 14; break;
            case "track-identity-collision": f.Track(4).Id = 9; break;
            case "nonlinear": f.Track(9).Flags = 0; break;
            case "track-flags": f.Track(9).Flags = 3; break;
            case "orphan": f.Track(10).RewardTrackIdParent = 4; break;
            case "branch": f.Track(11).RewardTrackIdParent = 9; break;
            case "extra-root": f.Track(10).RewardTrackIdParent = 0; break;
            case "disconnected-cycle": f.Track(10).RewardTrackIdParent = 11; break;
            case "empty-costs": f.Tracks[f.Tracks.IndexOf(f.Track(9))] = new() { Id = 9, RewardTrackTypeEnum = 2, Flags = 1 }; break;
            case "cost-hole": f.Track(9).RewardPointCost02 = 0; break;
            case "decreasing-cost": f.Track(9).RewardPointCost01 = 799; break;
            case "extra-cost": f.Track(9).RewardPointCost06 = 5000; break;
            case "missing-slot": f.Rewards.Remove(f.Reward(312)); break;
            case "extra-slot": f.Reward(312).RewardPointFlags = 64; break;
            case "zero-mask": f.Reward(312).RewardPointFlags = 0; break;
            case "multiple-mask-bits": f.Reward(312).RewardPointFlags = 3; break;
            case "duplicate-reward-id": f.Rewards.Add(f.Reward(276)); break;
            case "reward-wire-overflow": f.Reward(276).Id = 1u << 14; break;
            case "ambiguous-slot": var row = Clone(f.Reward(276)); row.Id = 999; f.Rewards.Add(row); break;
            case "nonfinal-casque": f.Reward(276).Flags = 7; break;
            case "final-not-casque": f.Reward(279).Flags = 6; break;
            case "reward-flags": f.Reward(276).Flags = 14; break;
            case "reward-currency-type": f.Reward(276).CurrencyTypeId = 1; break;
            case "reward-currency-amount": f.Reward(276).CurrencyAmount = 1; break;
            case "item2-choice": f.Reward(276).RewardTrackRewardTypeEnum00 = 0; break;
            case "missing-choice": f.Reward(276).RewardChoiceId00 = 0; break;
            case "zero-count": f.Reward(276).RewardChoiceCount00 = 0; break;
            case "multiple-count": f.Reward(276).RewardChoiceCount00 = 2; break;
            case "second-choice": f.Reward(276).RewardChoiceId01 = 103; break;
            case "empty-second-type": f.Reward(276).RewardTrackRewardTypeEnum01 = 1; break;
            case "empty-second-count": f.Reward(276).RewardChoiceCount01 = 1; break;
            case "third-choice": f.Reward(276).RewardChoiceId02 = 103; break;
            case "missing-account-item": f.Items.RemoveAll(i => i.Id == 103); break;
            default: throw new ArgumentOutOfRangeException(nameof(scenario));
        }
        Assert.Throws<InvalidDataException>(() => f.Build());
    }

    [Theory]
    [InlineData(nameof(IGameTableManager.RewardTrack))]
    [InlineData(nameof(IGameTableManager.RewardTrackRewards))]
    [InlineData(nameof(IGameTableManager.AccountItem))]
    public void MissingTablesRejectIncompleteCatalog(string missing)
    {
        var f = new Fixture();
        Assert.Throws<InvalidDataException>(() => f.Build(missing));
        Assert.Throws<ArgumentNullException>(() => new LoyaltyCatalog(null));
    }

    private static RewardTrackRewardsEntry Clone(RewardTrackRewardsEntry entry) =>
        (RewardTrackRewardsEntry)typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(entry, null)!;

    private sealed class Fixture
    {
        public List<RewardTrackEntry> Tracks { get; } = LoyaltyCatalogBuild16042Fixture.Tracks().ToList();
        public List<RewardTrackRewardsEntry> Rewards { get; } = LoyaltyCatalogBuild16042Fixture.Rewards().ToList();
        public List<AccountItemEntry> Items { get; } = LoyaltyCatalogBuild16042Fixture.AccountItems().ToList();
        public RewardTrackEntry Track(uint id) => Tracks.Single(t => t.Id == id);
        public RewardTrackRewardsEntry Reward(uint id) => Rewards.Single(r => r.Id == id);
        public LoyaltyCatalog Build(string missing = null)
        {
            var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var proxy);
            if (missing != nameof(IGameTableManager.RewardTrack)) proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(Tracks.ToArray()));
            if (missing != nameof(IGameTableManager.RewardTrackRewards)) proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(Rewards.ToArray()));
            if (missing != nameof(IGameTableManager.AccountItem)) proxy.SetProperty(nameof(IGameTableManager.AccountItem), PlayerBindPointTests.Table(Items.ToArray()));
            return new LoyaltyCatalog(tables);
        }
    }
}
