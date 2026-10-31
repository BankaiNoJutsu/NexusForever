using System.Numerics;
using NexusForever.Game.Quest;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Game.Quest.Reward;
using System.Text.Json.Nodes;

namespace NexusForever.Game.Tests.Quest;

public sealed class ContractRewardCatalogTests
{
    [Theory]
    [InlineData(4u)] [InlineData(5u)]
    public void ChooseSelectionProvenanceRecordsAnOperationWithoutInventingQuestAcceptance(uint track)
    {
        var catalog = new ContractRewardCatalog(new Fixture().Tables); string operation = Guid.NewGuid().ToString("N");
        var satisfied = catalog.GetTrack(track).Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Append(catalog.GetTrack(track).PrerequisiteId);
        var selected = ContractRewardSelection.CaptureForChoose(catalog, track, 7, operation, satisfied, _ => 0);
        var restored = ContractRewardSelection.Restore(catalog, selected.Json); Assert.Null(restored.AcceptanceId); Assert.Equal(operation, restored.OperationId);
        Assert.Equal(selected.Rewards, restored.Rewards); Assert.Equal(7ul, restored.CycleId); Assert.DoesNotContain("AcceptanceId", restored.Json);
    }

    [Theory]
    [InlineData("operation")] [InlineData("history")] [InlineData("reward")] [InlineData("policy")] [InlineData("version")] [InlineData("unknown-field")]
    public void ChooseProvenanceCannotBeReboundOrSmuggleAcceptanceAndUnknownFields(string fault)
    {
        var catalog = new ContractRewardCatalog(new Fixture().Tables);
        var satisfied = catalog.GetTrack(5).Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Distinct();
        var selected = ContractRewardSelection.CaptureForChoose(catalog, 5, 0, Guid.NewGuid().ToString("N"), satisfied, _ => 0);
        var payload = JsonNode.Parse(selected.Json).AsObject();
        if (fault == "operation") payload["OperationId"] = "invalid";
        if (fault == "history") payload["AcceptanceId"] = Guid.NewGuid().ToString("N");
        if (fault == "reward") payload["Rewards"][0] = 69;
        if (fault == "policy") payload["Policy"] = "unmapped";
        if (fault == "version") payload["Version"] = 3;
        if (fault == "unknown-field") payload["Unknown"] = 1;
        Assert.Throws<InvalidOperationException>(() => ContractRewardSelection.Restore(catalog, payload.ToJsonString()));
    }

    [Theory]
    [MemberData(nameof(ActualRows))]
    public void EveryActualRowCanBeCapturedAndRestoredOnlyInItsEligibleOrdinalWithoutLosingItsChoices(uint rowId)
    {
        var catalog = new ContractRewardCatalog(new Fixture().Tables); var reference = catalog.GetReward(rowId);
        uint[] satisfied = reference.Track.Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Where(id => id != 0).Distinct().ToArray();
        int ordinal = 0;
        var selection = ContractRewardSelection.Capture(catalog, reference.Track.Id, 7, "123456781234123412341234567890ab", satisfied, count =>
        {
            byte current = (byte)ordinal++; var eligible = catalog.EligibleCandidates(reference.Track.Id, current, satisfied.ToHashSet());
            return current == reference.Slot.Ordinal ? eligible.Select(row => row.Id).ToList().IndexOf(rowId) : 0;
        });
        var restored = ContractRewardSelection.Restore(catalog, selection.Json);
        Assert.Equal(rowId, restored.Rewards[reference.Slot.Ordinal]); Assert.Equal(selection.Json, restored.Json);
        Assert.Equal(7ul, restored.CycleId); Assert.Equal(reference.Track.Slots.Count, ordinal);
        Assert.Equal(3, catalog.GetReward(restored.Rewards[reference.Slot.Ordinal]).Reward.Choices.Count);
    }

    [Theory]
    [InlineData(4)] [InlineData(5)]
    public void AnEmptyEligiblePoolCannotFallBackToAnotherClassRankOrUnconditionalFinal(uint track)
    {
        var catalog = new ContractRewardCatalog(new Fixture().Tables);
        Assert.Throws<InvalidOperationException>(() => ContractRewardSelection.Capture(catalog, track, 0, "123456781234123412341234567890ab", []));
    }

    [Theory]
    [InlineData(-1)] [InlineData(int.MaxValue)]
    public void ASelectorCannotEscapeItsEvaluatedPool(int index)
    {
        var catalog = new ContractRewardCatalog(new Fixture().Tables);
        var satisfied = catalog.GetTrack(5).Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Distinct();
        Assert.Throws<InvalidOperationException>(() => ContractRewardSelection.Capture(catalog, 5, 0, "123456781234123412341234567890ab", satisfied, _ => index));
    }

    [Theory]
    [InlineData("version")] [InlineData("policy")] [InlineData("owner")] [InlineData("acceptance")]
    [InlineData("missing-ordinal")] [InlineData("wrong-ordinal")] [InlineData("eligibility")] [InlineData("noncanonical")]
    public void PersistedSelectionProvenanceRejectsIncompleteOrConflictingInputs(string fault)
    {
        var catalog = new ContractRewardCatalog(new Fixture().Tables);
        var satisfied = catalog.GetTrack(5).Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Distinct();
        var selection = ContractRewardSelection.Capture(catalog, 5, 0, "123456781234123412341234567890ab", satisfied, _ => 0);
        var payload = JsonNode.Parse(selection.Json).AsObject();
        switch (fault)
        {
            case "version": payload["Version"] = 2; break;
            case "policy": payload["Policy"] = "unknown"; break;
            case "owner": payload["TrackId"] = 35; break;
            case "acceptance": payload["AcceptanceId"] = "invalid"; break;
            case "missing-ordinal": payload["Rewards"].AsArray().RemoveAt(0); break;
            case "wrong-ordinal": payload["Rewards"][0] = 69; break;
            case "eligibility": payload["Satisfied"] = new JsonArray(); break;
        }
        string json = fault == "noncanonical" ? " " + selection.Json : payload.ToJsonString();
        Assert.Throws<InvalidOperationException>(() => ContractRewardSelection.Restore(catalog, json));
    }

    [Fact]
    public void CapturedSelectionCannotBeChangedByLaterPrerequisitesOrCallerListMutation()
    {
        var catalog = new ContractRewardCatalog(new Fixture().Tables);
        var satisfied = catalog.GetTrack(5).Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Distinct().ToList();
        var selection = ContractRewardSelection.Capture(catalog, 5, 0, "123456781234123412341234567890ab", satisfied, _ => 0);
        string json = selection.Json; satisfied.Clear(); Assert.Equal(json, selection.Json);
        Assert.Throws<NotSupportedException>(() => ((IList<uint>)selection.Rewards)[0] = 69);
        Assert.Equal(selection.Rewards, ContractRewardSelection.Restore(catalog, json).Rewards);
    }

    public static IEnumerable<object[]> ActualRows() => LoyaltyCatalogBuild16042Fixture.Rewards()
        .Where(r => r.RewardTrackId is 4 or 5).Select(r => new object[] { r.Id });

    [Theory]
    [MemberData(nameof(ActualRows))]
    public void EveryActualContractRewardRetainsItsOrdinalPrerequisiteAndEveryChoiceWithoutConvertingAccountItems(uint rowId)
    {
        var f = new Fixture(); var catalog = new ContractRewardCatalog(f.Tables);
        var source = f.Rows.Single(r => r.Id == rowId); var row = catalog.GetReward(rowId);
        Assert.NotNull(row); Assert.Equal(source.RewardTrackId, row.Track.Id);
        Assert.Equal((byte)BitOperations.TrailingZeroCount(source.RewardPointFlags), row.Slot.Ordinal);
        Assert.Equal(source.PrerequisiteId, row.Reward.PrerequisiteId); Assert.Equal(source.Flags, row.Reward.Flags);
        Assert.Equal(source.CurrencyTypeId, row.Reward.CurrencyTypeId); Assert.Equal(source.CurrencyAmount, row.Reward.CurrencyAmount);
        uint[] ids = [source.RewardChoiceId00, source.RewardChoiceId01, source.RewardChoiceId02];
        uint[] types = [source.RewardTrackRewardTypeEnum00, source.RewardTrackRewardTypeEnum01, source.RewardTrackRewardTypeEnum02];
        uint[] counts = [source.RewardChoiceCount00, source.RewardChoiceCount01, source.RewardChoiceCount02];
        Assert.Equal(ids.Count(id => id != 0), row.Reward.Choices.Count);
        foreach (var choice in row.Reward.Choices)
        {
            Assert.Equal(ids[choice.Index], choice.Id); Assert.Equal(types[choice.Index], choice.Type); Assert.Equal(counts[choice.Index], choice.Count);
            if (choice.Type == 1) Assert.Contains(choice.Id, new uint[] { 2485, 2486 });
        }
    }

    [Fact]
    public void BothCompleteCatalogsRetainAll242RowsAndTheirIndependentCostPositions()
    {
        var catalog = new ContractRewardCatalog(new Fixture().Tables);
        Assert.Equal(new uint[] { 4, 5 }, catalog.Tracks.Select(t => t.Id));
        Assert.Equal(242, catalog.Tracks.Sum(t => t.Slots.Sum(s => s.Candidates.Count)));
        Assert.Equal(new uint[] { 1000, 2000, 3000 }, catalog.GetTrack(4).Slots.Select(s => s.RequiredPoints));
        Assert.Equal(new uint[] { 1000, 2000, 3000, 4000, 5000 }, catalog.GetTrack(5).Slots.Select(s => s.RequiredPoints));
        Assert.Equal(270u, Assert.Single(catalog.GetTrack(4).Slots[^1].Candidates).Id);
        Assert.Equal(69u, Assert.Single(catalog.GetTrack(5).Slots[^1].Candidates).Id);
        foreach (uint id in new uint[] { 69, 270 })
        { Assert.Equal(1u, catalog.GetReward(id).Reward.CurrencyTypeId); Assert.Equal(2500000u, catalog.GetReward(id).Reward.CurrencyAmount); }
        Assert.Null(catalog.GetTrack(9)); Assert.Null(catalog.GetTrack(16)); Assert.Null(catalog.GetTrack(35));
    }

    [Fact]
    public void NoSatisfiedPrerequisiteCannotSelectAnotherClassOrRankButTheUnconditionalFinalSlotRemainsVisible()
    {
        var catalog = new ContractRewardCatalog(new Fixture().Tables);
        foreach (var track in catalog.Tracks)
        {
            foreach (var slot in track.Slots.Take(track.Slots.Count - 1)) Assert.Empty(catalog.EligibleCandidates(track.Id, slot.Ordinal, new HashSet<uint>()));
            Assert.Single(catalog.EligibleCandidates(track.Id, track.Slots[^1].Ordinal, new HashSet<uint>()));
        }
        var source = catalog.GetReward(17); var eligible = catalog.EligibleCandidates(5, source.Slot.Ordinal, new HashSet<uint> { source.Reward.PrerequisiteId });
        Assert.Contains(eligible, r => r.Id == 17); Assert.All(eligible, r => Assert.Equal(source.Reward.PrerequisiteId, r.PrerequisiteId));
    }

    [Fact]
    public void ParentTrackPrerequisiteIsAlsoRequiredAndAllReturnedCollectionsAreImmutableSnapshots()
    {
        var f = new Fixture(); var entry = f.Tracks.Single(t => t.Id == 5); entry.PrerequisiteId = 123;
        var catalog = new ContractRewardCatalog(f.Tables); var original = f.Rows.Single(r => r.Id == 17);
        Assert.Empty(catalog.EligibleCandidates(5, 0, new HashSet<uint> { 3 }));
        var eligible = catalog.EligibleCandidates(5, 0, new HashSet<uint> { 3, 123 }); Assert.NotEmpty(eligible);
        entry.PrerequisiteId = 999; entry.RewardPointCost00 = 1;
        original.PrerequisiteId = 999; original.RewardChoiceId00 = 1; original.RewardChoiceCount00 = 99;
        Assert.Equal(123u, catalog.GetTrack(5).PrerequisiteId); Assert.Equal(1000u, catalog.GetTrack(5).Slots[0].RequiredPoints);
        Assert.Equal(3u, catalog.GetReward(17).Reward.PrerequisiteId); Assert.Equal(72011u, catalog.GetReward(17).Reward.Choices[0].Id);
        Assert.Throws<NotSupportedException>(() => ((IList<ContractReward>)eligible).Clear());
        Assert.Throws<NotSupportedException>(() => ((IList<ContractRewardChoice>)catalog.GetReward(17).Reward.Choices).Clear());
    }

    [Theory]
    [InlineData("track-owner")] [InlineData("track-parent")] [InlineData("track-wire")] [InlineData("duplicate-type")]
    [InlineData("duplicate-track-id")] [InlineData("duplicate-reward-id")] [InlineData("cost-hole")] [InlineData("cost-order")]
    [InlineData("missing-slot")] [InlineData("bad-mask")] [InlineData("wrong-final")] [InlineData("bad-binding")]
    [InlineData("separate-currency")] [InlineData("choice-type")] [InlineData("choice-hole")] [InlineData("zero-count")]
    [InlineData("missing-item")] [InlineData("missing-account-item")]
    public void InvalidCatalogsNeverSilentlyDropChoicesOrNormalizeUnsupportedData(string change)
    {
        var f = new Fixture(); var track = f.Tracks.Single(t => t.Id == 5); var row = f.Rows.Single(r => r.Id == 17);
        switch (change)
        {
            case "track-owner": track.Flags = 1; break;
            case "track-parent": track.RewardTrackIdParent = 4; break;
            case "track-wire": track.Id = 0; break;
            case "duplicate-type": f.Tracks.Add(new() { Id = 100, RewardTrackTypeEnum = 1 }); break;
            case "duplicate-track-id": f.Tracks.Add(new() { Id = 5, RewardTrackTypeEnum = 2 }); break;
            case "duplicate-reward-id": f.Rows.Add(new() { Id = 17, RewardTrackId = 9 }); break;
            case "cost-hole": track.RewardPointCost01 = 0; break;
            case "cost-order": track.RewardPointCost01 = 1; break;
            case "missing-slot": f.Rows.RemoveAll(r => r.RewardTrackId == 5 && r.RewardPointFlags == 16); break;
            case "bad-mask": row.RewardPointFlags = 3; break;
            case "wrong-final": row.Flags = 3; break;
            case "bad-binding": row.Flags = 0; break;
            case "separate-currency": row.CurrencyTypeId = 1; row.CurrencyAmount = 0; break;
            case "choice-type": row.RewardTrackRewardTypeEnum00 = 2; break;
            case "choice-hole": row.RewardChoiceId00 = 0; row.RewardChoiceCount00 = 0; break;
            case "zero-count": row.RewardChoiceCount00 = 0; break;
            case "missing-item": f.Items.Clear(); break;
            case "missing-account-item": f.AccountItems.Clear(); break;
        }
        f.Publish(); Assert.Throws<InvalidDataException>(() => new ContractRewardCatalog(f.Tables));
    }

    [Fact]
    public void IncompleteDataAndInvalidOwnerQueriesDoNotInventAReward()
    {
        var f = new Fixture(); var catalog = new ContractRewardCatalog(f.Tables);
        Assert.Throws<KeyNotFoundException>(() => catalog.EligibleCandidates(35, 0, new HashSet<uint>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => catalog.EligibleCandidates(4, 3, new HashSet<uint>()));
        Assert.Throws<ArgumentNullException>(() => catalog.EligibleCandidates(4, 0, null));
        f.Proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), null);
        Assert.Throws<InvalidDataException>(() => new ContractRewardCatalog(f.Tables));
    }

    private sealed class Fixture
    {
        internal readonly IGameTableManager Tables;
        internal readonly RecordingDispatchProxy<IGameTableManager> Proxy;
        internal readonly List<RewardTrackEntry> Tracks = LoyaltyCatalogBuild16042Fixture.Tracks().ToList();
        internal readonly List<RewardTrackRewardsEntry> Rows = LoyaltyCatalogBuild16042Fixture.Rewards().ToList();
        // Only identity existence is interpreted by this static catalog. Actual
        // Item/AccountItem effects and delivery remain a separate transaction gate.
        internal readonly List<Item2Entry> Items;
        internal readonly List<AccountItemEntry> AccountItems = [new() { Id = 2485 }, new() { Id = 2486 }];
        internal Fixture()
        {
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out Proxy);
            Items = Rows.Where(r => r.RewardTrackId is 4 or 5)
                .SelectMany(r => new[] { (r.RewardTrackRewardTypeEnum00, r.RewardChoiceId00), (r.RewardTrackRewardTypeEnum01, r.RewardChoiceId01), (r.RewardTrackRewardTypeEnum02, r.RewardChoiceId02) })
                .Where(c => c.Item1 == 0 && c.Item2 != 0).Select(c => c.Item2).Distinct().Select(id => new Item2Entry { Id = id }).ToList(); Publish();
        }
        internal void Publish()
        {
            Proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(Tracks.ToArray()));
            Proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(Rows.ToArray()));
            Proxy.SetProperty(nameof(IGameTableManager.Item), PlayerBindPointTests.Table(Items.ToArray()));
            Proxy.SetProperty(nameof(IGameTableManager.AccountItem), PlayerBindPointTests.Table(AccountItems.ToArray()));
            Proxy.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1 }));
        }
    }
}
