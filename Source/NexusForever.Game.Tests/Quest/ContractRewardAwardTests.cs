using System.Text.Json.Nodes;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Account;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public sealed class ContractRewardAwardTests
{
    [Fact]
    public void APreviouslyCapturedCatalogResolvesPayoutsWithoutReadingLaterMutableTableMetadata()
    {
        var fixture = new Fixture(); var captured = new NexusForever.Game.Quest.ContractRewardCatalog(fixture.Tables);
        fixture.AccountItems[0].AccountCurrencyAmount = 99; fixture.AccountCurrency.Id = 199; fixture.Currency.CapAmount = 1;
        fixture.Publish(); var frozen = new ContractRewardAwardCatalog(captured);
        Assert.All(frozen.Awards.Where(row => row.AccountCurrency?.AccountItemId == 2485), row => Assert.Equal(5ul * row.ChoiceCount, row.AccountCurrency.Amount));
        Assert.Equal(0ul, frozen.GetAward(69, 0).CharacterCurrency.CapAmount);
        Assert.Throws<InvalidDataException>(() => fixture.Catalog());
    }

    public static IEnumerable<object[]> ActualChoices() => LoyaltyCatalogBuild16042Fixture.Rewards()
        .Where(row => row.RewardTrackId is 4 or 5).SelectMany(row => Enumerable.Range(0, 3).Select(choice => new object[] { row.Id, (uint)choice }));

    [Theory]
    [MemberData(nameof(ActualChoices))]
    public void EveryActualChoiceRetainsItsCompletePayoutAndSelectedCycleAcrossQuoteRestore(uint rowId, uint choice)
    {
        var fixture = new Fixture(); var catalog = fixture.Catalog(); var row = fixture.Rows.Single(row => row.Id == rowId);
        var award = catalog.GetAward(rowId, choice); var track = Selected(catalog, rowId);
        var quote = ContractRewardClaimQuote.Capture(catalog, 42, [track], track.TrackId, 7, 9, rowId, choice);
        Assert.Equal(award, quote.Award); Assert.Equal(42ul, quote.CharacterId); Assert.Equal(7ul, quote.CycleId);
        Assert.Equal(9ul, quote.Revision); Assert.Equal(track.Points, quote.Points); Assert.Equal(track.Slots[award.Ordinal].SelectionJson, quote.SelectionJson);
        var restored = ContractRewardClaimQuote.Restore(catalog, quote.Json); Assert.Equal(quote.Json, restored.Json); Assert.Equal(award, restored.Award);
        uint[] types = [row.RewardTrackRewardTypeEnum00, row.RewardTrackRewardTypeEnum01, row.RewardTrackRewardTypeEnum02];
        uint[] ids = [row.RewardChoiceId00, row.RewardChoiceId01, row.RewardChoiceId02];
        uint[] counts = [row.RewardChoiceCount00, row.RewardChoiceCount01, row.RewardChoiceCount02];
        Assert.Equal(row.RewardTrackId, award.TrackId); Assert.Equal(rowId, award.RewardRowId); Assert.Equal(choice, award.Choice);
        Assert.Equal(types[choice], award.ChoiceType); Assert.Equal(ids[choice], award.ChoiceId); Assert.Equal(counts[choice], award.ChoiceCount);
        if (types[choice] == 0)
        {
            Assert.Null(award.AccountCurrency); Assert.Equal(new ContractItemAward(ids[choice], counts[choice], true), award.Item);
        }
        else
        {
            Assert.Null(award.Item); var source = fixture.AccountItems.Single(row => row.Id == ids[choice]);
            Assert.Equal(new ContractAccountCurrencyAward(source.Id, AccountCurrencyType.ProtostarPromissoryNote,
                checked(source.AccountCurrencyAmount * counts[choice])), award.AccountCurrency);
        }
        if (row.CurrencyTypeId == 0) Assert.Null(award.CharacterCurrency);
        else Assert.Equal(new ContractCharacterCurrencyAward(CurrencyType.Credits, 2500000, 0), award.CharacterCurrency);
    }

    [Fact]
    public void BothCatalogsContainAll726ChoicesIncludingFinalCreditsAndPureAccountCurrencyCompensation()
    {
        var catalog = new Fixture().Catalog(); Assert.Equal(726, catalog.Awards.Count);
        Assert.Equal(242, catalog.Awards.Select(row => row.RewardRowId).Distinct().Count());
        Assert.Equal(new uint[] { 4, 5 }, catalog.Awards.Select(row => row.TrackId).Distinct().Order());
        Assert.All(catalog.Awards.Where(row => row.RewardRowId is 69 or 270), row =>
        {
            Assert.Equal(2500000ul, row.CharacterCurrency.Amount); Assert.NotNull(row.Item); Assert.Null(row.AccountCurrency);
        });
        Assert.Equal(6, catalog.Awards.Count(row => row.CharacterCurrency != null));
        Assert.Equal(new uint[] { 2485, 2486 }, catalog.Awards.Where(row => row.AccountCurrency != null).Select(row => row.AccountCurrency.AccountItemId).Distinct().Order());
        Assert.Throws<NotSupportedException>(() => ((IList<ContractRewardAward>)catalog.Awards).Clear());
        Assert.Null(catalog.GetAward(535, 0)); Assert.Null(catalog.GetAward(69, 3)); Assert.Null(catalog.GetAward(0, 0));
    }

    [Theory]
    [InlineData(2485u, 5ul)] [InlineData(2486u, 7ul)]
    public void CompensationMultipliesTheCanonicalAccountItemAmountAndNeverUsesACollidingItem2Id(uint accountItem, ulong amount)
    {
        var fixture = new Fixture(); var row = fixture.Rows.First(row => row.RewardChoiceId02 == accountItem);
        row.RewardChoiceCount02 = 3; fixture.Items.Add(new Item2Entry { Id = accountItem }); fixture.Publish();
        var award = fixture.Catalog().GetAward(row.Id, 2);
        Assert.Equal(new ContractAccountCurrencyAward(accountItem, AccountCurrencyType.ProtostarPromissoryNote, amount * 3), award.AccountCurrency);
        Assert.Null(award.Item); Assert.Equal(1u, award.ChoiceType); Assert.Equal(3u, award.ChoiceCount);
    }

    [Theory]
    [InlineData(69u)] [InlineData(270u)]
    public void FinalCurrencyIsAwardedOnceAlongsideAnyItemChoiceWithoutMultiplicationByItemQuantity(uint reward)
    {
        var fixture = new Fixture(); var row = fixture.Rows.Single(row => row.Id == reward); row.RewardChoiceCount00 = 11;
        var catalog = fixture.Catalog();
        for (uint choice = 0; choice < 3; ++choice)
        {
            var award = catalog.GetAward(reward, choice); Assert.Equal(2500000ul, award.CharacterCurrency.Amount);
            Assert.Equal(choice == 0 ? 11u : 1u, award.Item.Count); Assert.True(award.Item.ForceSoulbind);
        }
    }

    [Theory]
    [InlineData("flags")] [InlineData("item")] [InlineData("entitlement")] [InlineData("instant")]
    [InlineData("unlock")] [InlineData("prerequisite")] [InlineData("cooldown")] [InlineData("gacha")]
    [InlineData("zero-currency")] [InlineData("zero-amount")] [InlineData("unknown-currency")] [InlineData("currency-table")]
    [InlineData("currency-row")] [InlineData("overflow")]
    public void MixedUnknownOrIncompleteAccountEffectsCannotBeDroppedOrConvertedIntoAClaim(string fault)
    {
        var fixture = new Fixture(); var source = fixture.AccountItems[0];
        switch (fault)
        {
            case "flags": source.Flags = 2; break;
            case "item": source.Item2Id = 42; break;
            case "entitlement": source.EntitlementId = 44; break;
            case "instant": source.InstantEventEnum = 1; break;
            case "unlock": source.GenericUnlockSetId = 1; break;
            case "prerequisite": source.PrerequisiteId = 1; break;
            case "cooldown": source.AccountItemCooldownGroupId = 1; break;
            case "gacha": source.Creature2DisplayGroupIdGacha = 1; break;
            case "zero-currency": source.AccountCurrencyEnum = 0; break;
            case "zero-amount": source.AccountCurrencyAmount = 0; break;
            case "unknown-currency": source.AccountCurrencyEnum = 199; fixture.AccountCurrency.Id = 199; break;
            case "currency-row": fixture.AccountCurrency.Id = 15; break;
            case "overflow": source.AccountCurrencyAmount = ulong.MaxValue; fixture.Rows.First(row => row.RewardChoiceId02 == source.Id).RewardChoiceCount02 = 2; break;
        }
        fixture.Publish();
        if (fault == "currency-table") fixture.Proxy.SetProperty(nameof(IGameTableManager.AccountCurrencyType), null);
        Assert.Throws<InvalidDataException>(() => fixture.Catalog());
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void UnknownOrMissingCharacterCurrencyOwnersCannotBeCapturedAsAValidAward(bool missing)
    {
        var fixture = new Fixture();
        if (missing) fixture.Proxy.SetProperty(nameof(IGameTableManager.CurrencyType), null);
        else { fixture.Rows.Single(row => row.Id == 69).CurrencyTypeId = 199; fixture.Currency.Id = 199; fixture.Publish(); }
        Assert.Throws<InvalidDataException>(() => fixture.Catalog());
    }

    [Fact]
    public void CaptureAndRestoreUseFrozenEffectsInsteadOfMutableTableRowsOrCallerCollections()
    {
        var fixture = new Fixture(); var catalog = fixture.Catalog();
        uint reward = fixture.Rows.First(row => row.RewardChoiceId02 == 2485).Id; var track = Selected(catalog, reward);
        var tracks = new List<ContractTrackState> { track };
        var quote = ContractRewardClaimQuote.Capture(catalog, 42, tracks, track.TrackId, 7, 9, reward, 2); string json = quote.Json;
        tracks.Clear(); fixture.AccountItems[0].AccountCurrencyAmount = 99;
        fixture.Rows.Single(row => row.Id == reward).RewardChoiceCount02 = 5; fixture.Publish();
        Assert.Equal(5ul, quote.Award.AccountCurrency.Amount); Assert.Equal(json, ContractRewardClaimQuote.Restore(catalog, json).Json);
        Assert.Throws<InvalidOperationException>(() => ContractRewardClaimQuote.Restore(fixture.Catalog(), json));
    }

    [Theory]
    [InlineData("zero-owner")] [InlineData("foreign-owner")] [InlineData("missing-track")] [InlineData("inactive")]
    [InlineData("unselected")] [InlineData("cycle")] [InlineData("revision")] [InlineData("zero-revision")]
    [InlineData("unearned")] [InlineData("unselected-row")] [InlineData("other-track")] [InlineData("unknown-reward")]
    [InlineData("unknown-choice")] [InlineData("incomplete-slots")] [InlineData("provenance")] [InlineData("duplicate-owner")]
    public void AQuoteCannotBootstrapAnUnearnedStaleForeignOrUnselectedRequest(string fault)
    {
        var catalog = new Fixture().Catalog(); var track = Selected(catalog, 69);
        ulong character = 42, cycle = 7, revision = 9; uint trackId = 5, reward = 69, choice = 0;
        if (fault == "zero-owner") character = 0;
        if (fault == "foreign-owner") character = 43;
        if (fault == "missing-track") trackId = 4;
        if (fault == "inactive") track = track with { Active = false };
        if (fault == "unselected") track = track with { SelectionInitialized = false, Slots = Array.Empty<ContractRewardSlotState>() };
        if (fault == "cycle") cycle++;
        if (fault == "revision") revision++;
        if (fault == "zero-revision") track = track with { Revision = 0 };
        if (fault == "unearned") track = track with { Points = 4999 };
        if (fault == "unselected-row") reward = catalog.Catalog.GetTrack(5).Slots[0].Candidates.First(row => row.Id != track.Slots[0].RewardRowId).Id;
        if (fault == "other-track") reward = 270;
        if (fault == "unknown-reward") reward = 535;
        if (fault == "unknown-choice") choice = 3;
        if (fault == "incomplete-slots") track = track with { Slots = track.Slots.Skip(1).ToArray() };
        if (fault == "provenance") track = track with { CycleId = 8 };
        Assert.Throws<InvalidOperationException>(() => ContractRewardClaimQuote.Capture(catalog, character,
            fault == "duplicate-owner" ? [track, track] : [track], trackId, cycle, revision, reward, choice));
    }

    [Theory]
    [InlineData("version")] [InlineData("zero-owner")] [InlineData("zero-revision")] [InlineData("cycle")]
    [InlineData("unearned")] [InlineData("unknown-choice")] [InlineData("reward")] [InlineData("item-quantity")]
    [InlineData("item-binding")] [InlineData("drop-currency")] [InlineData("credits")]
    [InlineData("account-alias")] [InlineData("account-amount")] [InlineData("extra-field")] [InlineData("noncanonical")]
    public void QuoteRestoreRejectsChangedOrIncompletePayoutsAndSelectionProvenance(string fault)
    {
        var fixture = new Fixture(); var catalog = fixture.Catalog();
        uint reward = fault.StartsWith("account-", StringComparison.Ordinal) ? fixture.Rows.First(row => row.RewardChoiceId02 == 2485).Id : 69;
        var track = Selected(catalog, reward);
        var quote = ContractRewardClaimQuote.Capture(catalog, 42, [track], track.TrackId, 7, 9, reward, reward == 69 ? 0u : 2u);
        var payload = JsonNode.Parse(quote.Json).AsObject(); var award = payload["Award"].AsObject();
        switch (fault)
        {
            case "version": payload["Version"] = 2; break;
            case "zero-owner": payload["CharacterId"] = 0; break;
            case "zero-revision": payload["Revision"] = 0; break;
            case "cycle": payload["CycleId"] = 8; break;
            case "unearned": payload["Points"] = 0; break;
            case "unknown-choice": award["Choice"] = 3; break;
            case "reward": award["RewardRowId"] = 270; break;
            case "item-quantity": award["Item"]["Count"] = 99; break;
            case "item-binding": award["Item"]["ForceSoulbind"] = false; break;
            case "drop-currency": award["CharacterCurrency"] = null; break;
            case "credits": award["CharacterCurrency"]["Amount"] = 1; break;
            case "account-alias": award["ChoiceType"] = 0; break;
            case "account-amount": award["AccountCurrency"]["Amount"] = 99; break;
            case "extra-field": payload["ClaimId"] = Guid.NewGuid().ToString("N"); break;
        }
        Assert.Throws<InvalidOperationException>(() => ContractRewardClaimQuote.Restore(catalog,
            fault == "noncanonical" ? " " + quote.Json : payload.ToJsonString()));
    }

    [Fact]
    public void IndependentActiveContractTypesAndPriorSelectionEligibilityRemainIntactDuringQuoteCapture()
    {
        var fixture = new Fixture(); var catalog = fixture.Catalog(); var pvp = Selected(catalog, 270); var pve = Selected(catalog, 69);
        var tracks = new[] { pvp, pve };
        var first = ContractRewardClaimQuote.Capture(catalog, 42, tracks, 4, 7, 9, 270, 1);
        var second = ContractRewardClaimQuote.Capture(catalog, 42, tracks, 5, 7, 9, 69, 2);
        Assert.Equal(0u, first.Award.TrackType); Assert.Equal(1u, second.Award.TrackType);
        Assert.True(pvp.Active); Assert.True(pve.Active); Assert.Equal(pvp, tracks[0]); Assert.Equal(pve, tracks[1]);
    }

    private static ContractTrackState Selected(ContractRewardAwardCatalog catalog, uint reward)
    {
        var reference = catalog.Catalog.GetReward(reward); int ordinal = 0;
        var satisfied = reference.Track.Slots.SelectMany(slot => slot.Candidates).Select(row => row.PrerequisiteId).Append(reference.Track.PrerequisiteId).Where(id => id != 0).Distinct().ToHashSet();
        int Select(int count)
        {
            byte current = (byte)ordinal++;
            return current == reference.Slot.Ordinal ? catalog.Catalog.EligibleCandidates(reference.Track.Id, current, satisfied).Select(row => row.Id).ToList().IndexOf(reward) : 0;
        }
        string identity = "123456781234123412341234567890ab";
        var selection = reward % 2 == 0 ? ContractRewardSelection.Capture(catalog.Catalog, reference.Track.Id, 7, identity, satisfied, Select)
            : ContractRewardSelection.CaptureForChoose(catalog.Catalog, reference.Track.Id, 7, identity, satisfied, Select);
        return new(42, reference.Track.Id, 7, reference.Track.MaximumPoints + 17, 9, true)
        {
            SelectionInitialized = true,
            Slots = Array.AsReadOnly(reference.Track.Slots.Select(slot => new ContractRewardSlotState(slot.Ordinal, selection.Rewards[slot.Ordinal], slot.RequiredPoints, selection.Json)).ToArray())
        };
    }

    private sealed class Fixture
    {
        internal readonly IGameTableManager Tables;
        internal readonly RecordingDispatchProxy<IGameTableManager> Proxy;
        internal readonly List<RewardTrackRewardsEntry> Rows = LoyaltyCatalogBuild16042Fixture.Rewards().ToList();
        internal readonly List<Item2Entry> Items;
        // Exact grant fields from configured build16042 AccountItem.tbl, hash
        // 13A07FBBE1737851EBC67A88697D3CD80FD2281795AE85BFB49C3F79855510AA.
        // Cosmetic metadata omitted. No Item2 behavior is inferred from these.
        internal readonly List<AccountItemEntry> AccountItems =
        [new() { Id = 2485, AccountCurrencyEnum = 14, AccountCurrencyAmount = 5, EntitlementCount = 1 },
         new() { Id = 2486, AccountCurrencyEnum = 14, AccountCurrencyAmount = 7, EntitlementCount = 1 }];
        internal readonly CurrencyTypeEntry Currency = new() { Id = 1 };
        internal readonly AccountCurrencyTypeEntry AccountCurrency = new() { Id = 14 };
        internal Fixture()
        {
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out Proxy);
            Items = Rows.Where(row => row.RewardTrackId is 4 or 5)
                .SelectMany(row => new[] { (row.RewardTrackRewardTypeEnum00, row.RewardChoiceId00), (row.RewardTrackRewardTypeEnum01, row.RewardChoiceId01), (row.RewardTrackRewardTypeEnum02, row.RewardChoiceId02) })
                .Where(choice => choice.Item1 == 0).Select(choice => choice.Item2).Distinct().Select(id => new Item2Entry { Id = id }).ToList();
            Publish();
        }
        internal ContractRewardAwardCatalog Catalog() => new(Tables);
        internal void Publish()
        {
            Proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(LoyaltyCatalogBuild16042Fixture.Tracks()));
            Proxy.SetProperty(nameof(IGameTableManager.RewardTrackRewards), PlayerBindPointTests.Table(Rows.ToArray()));
            Proxy.SetProperty(nameof(IGameTableManager.Item), PlayerBindPointTests.Table(Items.ToArray()));
            Proxy.SetProperty(nameof(IGameTableManager.AccountItem), PlayerBindPointTests.Table(AccountItems.ToArray()));
            Proxy.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(Currency));
            Proxy.SetProperty(nameof(IGameTableManager.AccountCurrencyType), PlayerBindPointTests.Table(AccountCurrency));
        }
    }
}
