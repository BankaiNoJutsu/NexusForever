using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Tests.Account.Reward;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;

namespace NexusForever.Game.Tests.Quest;

public sealed class ContractRewardSourceTests
{
    public static IEnumerable<object[]> ActualSources() => ContractBuild16042Fixture.Quests().Select(q => new object[]
    {
        q.Id,
        q.PeriodicQuestGroupId is 49 or 50 or 51 ? 4u : q.PeriodicQuestGroupId is 52 or 53 or 54 ? 5u : 0u,
        q.PeriodicQuestGroupId is 49 or 52 ? 50u : q.PeriodicQuestGroupId is 50 or 53 ? 150u : q.PeriodicQuestGroupId is 51 or 54 ? 450u : 0u
    });

    [Theory]
    [MemberData(nameof(ActualSources))]
    public void EveryActualContractUsesItsCanonicalTypeAndConfiguredFormulaOrRetainsItsMissingGroupBlocker(uint questId, uint trackId, uint points)
    {
        var f = new Fixture(); var manager = new ContractManager(f.Tables); var info = f.Info(questId);
        Assert.Equal(trackId != 0, manager.TryGetRewardSource(info, out var source));
        if (trackId == 0) { Assert.Null(source); Assert.False(manager.CanUseReceiverlessLifecycle(info)); return; }
        Assert.Equal((ushort)questId, source.QuestId); Assert.Equal(info.Entry.PeriodicQuestGroupId, source.PeriodicQuestGroupId);
        Assert.Equal(trackId, source.TrackId); Assert.Equal(trackId == 4 ? 0u : 1u, source.TrackType);
        Assert.Equal(points, source.BasePoints); Assert.False(source.UsesFormulaFallback);
        Assert.Equal(0u, source.TrackPrerequisiteId); Assert.True(manager.CanUseReceiverlessLifecycle(info));
    }

    [Theory]
    [InlineData(1u, 20u, false)] [InlineData(2u, 100u, false)] [InlineData(3u, 200u, false)] [InlineData(4u, 500u, false)]
    [InlineData(1u, 20u, true)] [InlineData(2u, 100u, true)] [InlineData(3u, 200u, true)] [InlineData(4u, 500u, true)]
    public void MissingFormulaUsesTheMappedClientFallbackPerQualityRatherThanTheConfiguredDataset(uint quality, uint points, bool missingTable)
    {
        var f = new Fixture(); f.Group(52).ContractQualityEnum = quality;
        f.Proxy.SetProperty(nameof(IGameTableManager.GameFormula), missingTable ? null : PlayerBindPointTests.Table<GameFormulaEntry>());
        Assert.True(new ContractManager(f.Tables).TryGetRewardSource(f.Info(9880), out var source));
        Assert.Equal(points, source.BasePoints); Assert.True(source.UsesFormulaFallback);
    }

    [Theory]
    [InlineData(0u)] [InlineData(2147483647u)]
    public void AnExplicitNonnegativeNativeIntFormulaValueIsNotReplacedByFallback(uint points)
    {
        var f = new Fixture(); f.Formula.Dataint0 = points;
        Assert.True(new ContractManager(f.Tables).TryGetRewardSource(f.Info(9880), out var source));
        Assert.Equal(points, source.BasePoints); Assert.False(source.UsesFormulaFallback);
    }

    [Theory]
    [InlineData("unknown-type")] [InlineData("zero-type")] [InlineData("zero-quality")] [InlineData("unknown-quality")]
    [InlineData("negative-formula")] [InlineData("missing-track")] [InlineData("missing-table")] [InlineData("multiple-roots")]
    [InlineData("duplicate-id")] [InlineData("account-owner")] [InlineData("parent")] [InlineData("wire-id")]
    public void UnmappedOrAmbiguousPointSourcesDoNotChangeTheExistingValidatedContractLifecycle(string change)
    {
        var f = new Fixture(); var group = f.Group(52); var track = f.Tracks.Single(t => t.Id == 5);
        switch (change)
        {
            case "unknown-type": group.ContractTypeEnum = 3; break;
            case "zero-type": group.ContractTypeEnum = 0; break;
            case "zero-quality": group.ContractQualityEnum = 0; break;
            case "unknown-quality": group.ContractQualityEnum = 5; break;
            case "negative-formula": f.Formula.Dataint0 = uint.MaxValue; break;
            case "missing-track": f.Tracks.Remove(track); break;
            case "missing-table": f.Proxy.SetProperty(nameof(IGameTableManager.RewardTrack), null); break;
            case "multiple-roots": f.Tracks.Add(new() { Id = 100, RewardTrackTypeEnum = 1 }); break;
            case "duplicate-id": f.Tracks.Add(new() { Id = 5, RewardTrackTypeEnum = 2 }); break;
            case "account-owner": track.Flags = 1; break;
            case "parent": track.RewardTrackIdParent = 4; break;
            case "wire-id": track.Id = 0x4000; break;
        }
        if (change != "missing-table") f.PublishTracks();
        var manager = new ContractManager(f.Tables); var info = f.Info(9880);
        Assert.False(manager.TryGetRewardSource(info, out var source)); Assert.Null(source);
        Assert.True(manager.CanUseReceiverlessLifecycle(info)); Assert.NotNull(manager.GetPeriodicQuestGroup(info));
    }

    [Fact]
    public void AQuoteCopiesCanonicalScalarsRatherThanFollowingLaterMutableTablesOrCallerMetadata()
    {
        var f = new Fixture(); var manager = new ContractManager(f.Tables); var info = f.Info(9880);
        Assert.True(manager.TryGetRewardSource(info, out var source));
        f.Formula.Dataint0 = 999; f.Group(52).ContractQualityEnum = 4; f.Tracks.Single(t => t.Id == 5).Id = 300;
        info.Entry.PeriodicQuestGroupId = 49;
        Assert.True(manager.TryGetRewardSource(info, out var retained)); Assert.Same(source, retained);
        Assert.Equal(52u, retained.PeriodicQuestGroupId); Assert.Equal(5u, retained.TrackId);
        Assert.Equal(1u, retained.Quality); Assert.Equal(50u, retained.BasePoints);
    }

    [Fact]
    public void TrackPrerequisiteIsRetainedWithoutPretendingTheStaticQuoteChecksPlayerEligibility()
    {
        var f = new Fixture(); f.Tracks.Single(t => t.Id == 5).PrerequisiteId = 123;
        Assert.True(new ContractManager(f.Tables).TryGetRewardSource(f.Info(9880), out var source));
        Assert.Equal(123u, source.TrackPrerequisiteId);
    }

    [Theory]
    [InlineData("null")] [InlineData("not-contract")] [InlineData("foreign-id")] [InlineData("missing-objective")]
    [InlineData("missing-reward")] [InlineData("missing-item")]
    public void ExistingContractDefinitionValidationRemainsRequiredBeforeDescribingPoints(string change)
    {
        var f = new Fixture(); IQuestInfo info = f.Info(9880);
        switch (change)
        {
            case "null": info = null; break;
            case "not-contract": ((RecordingDispatchProxy<IQuestInfo>)(object)info).SetMethodReturn(nameof(IQuestInfo.IsContract), false); break;
            case "foreign-id": info.Entry.Id = 9999; break;
            case "missing-objective": f.Proxy.SetProperty(nameof(IGameTableManager.QuestObjective), PlayerBindPointTests.Table<QuestObjectiveEntry>()); break;
            case "missing-reward": f.Proxy.SetProperty(nameof(IGameTableManager.Quest2Reward), PlayerBindPointTests.Table<Quest2RewardEntry>()); break;
            case "missing-item": f.Proxy.SetProperty(nameof(IGameTableManager.Item), PlayerBindPointTests.Table<Item2Entry>()); break;
        }
        Assert.False(new ContractManager(f.Tables).TryGetRewardSource(info, out var source)); Assert.Null(source);
    }

    [Fact]
    public void ExistingCustomManagersHaveNoImplicitPointAuthority()
    {
        IContractManager manager = new ExistingCustomManager();
        Assert.False(manager.TryGetRewardSource(null, out var source)); Assert.Null(source);
    }

    private sealed class ExistingCustomManager : IContractManager
    {
        public bool CanUseReceiverlessLifecycle(IQuestInfo info) => true;
        public bool CanAccept(IQuestInfo info, IEnumerable<IQuest> active, IEnumerable<IQuest> completed, DateTime now) => true;
        public uint[] GetGoodQualityContractQuestIds(IEnumerable<IQuest> active, IEnumerable<IQuest> completed, DateTime now) => [];
        public PeriodicQuestGroupEntry GetPeriodicQuestGroup(IQuestInfo info) => null;
    }

    private sealed class Fixture
    {
        internal readonly IGameTableManager Tables;
        internal readonly RecordingDispatchProxy<IGameTableManager> Proxy;
        internal readonly GameFormulaEntry Formula = ContractBuild16042Fixture.Formula();
        internal readonly List<RewardTrackEntry> Tracks = LoyaltyCatalogBuild16042Fixture.Tracks().ToList();
        private readonly Quest2Entry[] quests = ContractBuild16042Fixture.Quests();
        private readonly PeriodicQuestGroupEntry[] groups = ContractBuild16042Fixture.Groups();
        internal Fixture()
        {
            Tables = RecordingDispatchProxy<IGameTableManager>.Create(out Proxy);
            Proxy.SetProperty(nameof(IGameTableManager.Quest2), PlayerBindPointTests.Table(quests));
            Proxy.SetProperty(nameof(IGameTableManager.PeriodicQuestGroup), PlayerBindPointTests.Table(groups));
            Proxy.SetProperty(nameof(IGameTableManager.QuestObjective), PlayerBindPointTests.Table(ContractBuild16042Fixture.Objectives()));
            var rewards = ContractBuild16042Fixture.Rewards();
            Proxy.SetProperty(nameof(IGameTableManager.Quest2Reward), PlayerBindPointTests.Table(rewards));
            Proxy.SetProperty(nameof(IGameTableManager.Item), PlayerBindPointTests.Table(rewards.Where(r => r.Quest2RewardTypeId == 1).Select(r => r.ObjectId).Distinct().Select(id => new Item2Entry { Id = id }).ToArray()));
            Proxy.SetProperty(nameof(IGameTableManager.GameFormula), PlayerBindPointTests.Table(Formula)); PublishTracks();
        }
        internal void PublishTracks() => Proxy.SetProperty(nameof(IGameTableManager.RewardTrack), PlayerBindPointTests.Table(Tracks.ToArray()));
        internal PeriodicQuestGroupEntry Group(uint id) => groups.Single(g => g.Id == id);
        internal IQuestInfo Info(uint id)
        {
            var info = RecordingDispatchProxy<IQuestInfo>.Create(out var proxy);
            proxy.SetProperty(nameof(IQuestInfo.Entry), quests.Single(q => q.Id == id)); proxy.SetMethodReturn(nameof(IQuestInfo.IsContract), true); return info;
        }
    }
}
