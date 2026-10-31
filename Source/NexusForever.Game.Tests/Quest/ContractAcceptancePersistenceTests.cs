using System.Collections.Immutable;
using System.Data;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using GameQuest = NexusForever.Game.Quest.Quest;
using SourceFixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed class ContractAcceptancePersistenceTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ContractAcceptancePersistenceTests() { }
    internal ContractAcceptancePersistenceTests(DbContextOptions<CharacterContext> options) => this.options = options;

    [Fact]
    public async Task ActualAcceptancePersistsOneIdentityAcrossProgressDuplicateActiveAddsAndFreshManagerRelog()
    {
        using var f = new Fixture(options); using (var manager = f.Manager())
        {
            manager.QuestMention(f.Info); Assert.Null(f.Any(manager).ContractAcceptanceId);
            manager.QuestAdd(f.Info); var quest = f.Active(manager); string id = quest.ContractAcceptanceId;
            Assert.True(GameQuest.ValidContractAcceptanceId(id)); await f.Save(manager);
            manager.QuestAdd(f.Info); Assert.Equal(id, f.Active(manager).ContractAcceptanceId);
            manager.QuestAchieve(SourceFixture.QuestId); Assert.Equal(id, quest.ContractAcceptanceId);
            await f.Save(manager); Assert.Equal(id, f.Read().ContractAcceptanceId);
        }
        using var reloaded = f.Manager(load: true); string loaded = f.Active(reloaded).ContractAcceptanceId;
        reloaded.QuestAdd(f.Info); await f.Save(reloaded); Assert.Equal(loaded, f.Read().ContractAcceptanceId);
        f.AssertNoAwards();
    }

    [Theory]
    [InlineData("abandon")] [InlineData("complete")] [InlineData("botch")]
    public async Task ARealLaterAcceptanceGetsANewIdentityWithoutClearingTheEarlierTerminalIdentityEarly(string ending)
    {
        using var f = new Fixture(options); using var manager = f.Manager(); manager.QuestAdd(f.Info);
        var quest = f.Active(manager); string first = quest.ContractAcceptanceId; await f.Save(manager);
        f.End(manager, ending); Assert.Equal(first, quest.ContractAcceptanceId);
        await f.Save(manager);
        if (ending == "complete")
        {
            Assert.Equal(first, f.Read().ContractAcceptanceId);
            Assert.Throws<QuestException>(() => manager.QuestAdd(SourceFixture.QuestId, null));
            Assert.Equal(first, quest.ContractAcceptanceId);
            quest.Reset = DateTime.UtcNow.AddSeconds(-1);
        }
        manager.QuestAdd(f.Info); string next = f.Active(manager).ContractAcceptanceId;
        Assert.True(GameQuest.ValidContractAcceptanceId(next)); Assert.NotEqual(first, next);
        await f.Save(manager); Assert.Equal(next, f.Read().ContractAcceptanceId); f.AssertNoAwards();
    }

    [Theory]
    [InlineData((byte)QuestState.Accepted)] [InlineData((byte)QuestState.Achieved)]
    public async Task HistoricalActiveNullRowsRemainNullUntilTheyEndAndANewAcceptanceOccurs(byte state)
    {
        using var f = new Fixture(options); f.Seed(state, null); using var manager = f.Manager(load: true);
        Assert.Null(f.Active(manager).ContractAcceptanceId); manager.QuestAdd(f.Info);
        Assert.Null(f.Active(manager).ContractAcceptanceId); await f.Save(manager); Assert.Null(f.Read().ContractAcceptanceId);
        manager.QuestAbandon(SourceFixture.QuestId); manager.QuestAdd(f.Info);
        Assert.True(GameQuest.ValidContractAcceptanceId(f.Active(manager).ContractAcceptanceId)); await f.Save(manager);
        f.AssertNoAwards();
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NormalSaveCommitOrAcknowledgementFailureCannotMintAnotherAcceptance(bool after)
    {
        using var f = new Fixture(options); using var manager = f.Manager(); manager.QuestAdd(f.Info);
        string id = f.Active(manager).ContractAcceptanceId;
        await using (var context = f.Context(new CommitFault(after)))
        {
            await using var unit = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            manager.Save(context); await context.SaveChangesAsync(); await Assert.ThrowsAsync<IOException>(() => unit.CommitAsync());
        }
        Assert.Equal(id, f.Active(manager).ContractAcceptanceId); await f.Save(manager);
        Assert.Equal(id, f.Read().ContractAcceptanceId); Assert.False(f.Active(manager).NeedsPersistenceSave); f.AssertNoAwards();
    }

    [Fact]
    public async Task AReacceptanceDuringAnOlderFrozenNormalSaveSurvivesItsAcknowledgement()
    {
        using var f = new Fixture(options); using var manager = f.Manager(); manager.QuestAdd(f.Info); await f.Save(manager);
        string first = f.Active(manager).ContractAcceptanceId;
        f.Active(manager).Timer = 700;
        using (var frozen = manager.CaptureNormalSave(f.Database))
        {
            f.End(manager, "complete"); f.Any(manager).Reset = DateTime.UtcNow.AddSeconds(-1); manager.QuestAdd(f.Info);
            string next = f.Active(manager).ContractAcceptanceId; Assert.NotEqual(first, next);
            await using var context = f.Context(); frozen.Save(context); await context.SaveChangesAsync();
            Assert.Equal(first, f.Read().ContractAcceptanceId); Assert.Equal(next, f.Active(manager).ContractAcceptanceId);
            Assert.True(f.Active(manager).NeedsPersistenceSave);
        }
        await f.Save(manager); Assert.Equal(f.Active(manager).ContractAcceptanceId, f.Read().ContractAcceptanceId); f.AssertNoAwards();
    }

    [Fact]
    public async Task SharedChallengeQuestCaptureCarriesTheTokenAndPreservesALaterAcceptedGeneration()
    {
        using var f = new Fixture(options); await f.Inner.Source.Begin(16); await f.Inner.Source.Medal(16, 0, new(1, 1)); await f.Inner.Source.Medal(16, 1, new(1, 2));
        using var manager = f.Manager(); manager.QuestAdd(f.Info); await f.Save(manager); string first = f.Active(manager).ContractAcceptanceId;
        Assert.True(manager.TryCaptureRewardCompletion(f.Database, () => manager.ObjectiveUpdate(QuestObjectiveType.CompleteChallenge, 0, 1), out var capture));
        using (capture)
        {
            var prepared = await capture.PrepareAsync(() => f.Context()); Assert.Equal(first, prepared.Changes[0].After.Quest.ContractAcceptanceId);
            f.End(manager, "complete"); f.Any(manager).Reset = DateTime.UtcNow.AddSeconds(-1); manager.QuestAdd(f.Info);
            string next = f.Active(manager).ContractAcceptanceId;
            var before = f.Inner.Source.State(16); var command = CharacterChallengeRewardCommand.Credit(before,
                before with { Activated = false, Tier = 2, LastRewardTier = 2, Completions = before.Completions + 1, ActiveSeconds = 0 }, 2, new(1, 3), DateTime.UtcNow);
            var result = await capture.ExecuteAsync(new CharacterChallengeRewardExecutor(() => f.Context(), f.Inner.Source.Catalog, f.Inner.Source.Tables), prepared, command);
            Assert.True(result.Outcome == CharacterChallengeRewardExecutionOutcome.Committed, result.Error?.ToString());
            Assert.True(capture.AdoptCommitted(result)); Assert.Equal(first, f.Read().ContractAcceptanceId);
            Assert.Equal(next, f.Active(manager).ContractAcceptanceId); Assert.True(f.Active(manager).NeedsPersistenceSave);
        }
        await f.Save(manager); Assert.Equal(f.Active(manager).ContractAcceptanceId, f.Read().ContractAcceptanceId);
    }

    [Fact]
    public async Task AForeignStoredGenerationCannotBeOverwrittenByAnOlderLoadedOwner()
    {
        using var f = new Fixture(options); string first = Guid.NewGuid().ToString("N"), foreign = Guid.NewGuid().ToString("N"); f.Seed((byte)QuestState.Accepted, first);
        using var manager = f.Manager(load: true); f.Active(manager).Flags = QuestStateFlags.Tracked;
        await using (var writer = f.Context()) await writer.CharacterQuest.ExecuteUpdateAsync(set => set.SetProperty(q => q.ContractAcceptanceId, foreign));
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Save(manager)); Assert.Equal(foreign, f.Read().ContractAcceptanceId);
    }

    [Fact]
    public async Task TheColumnAlsoRejectsAWriterWhichBecameStaleAfterItsRowWasRead()
    {
        using var f = new Fixture(options); string first = Guid.NewGuid().ToString("N"), foreign = Guid.NewGuid().ToString("N"); f.Seed((byte)QuestState.Accepted, first);
        await using var stale = f.Context(); var row = await stale.CharacterQuest.SingleAsync();
        await using (var writer = f.Context()) await writer.CharacterQuest.ExecuteUpdateAsync(set => set.SetProperty(q => q.ContractAcceptanceId, foreign));
        row.Flags = 31; await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => stale.SaveChangesAsync());
        Assert.Equal(foreign, f.Read().ContractAcceptanceId); Assert.NotEqual((byte)31, f.Read().Flags);
    }

    [Fact]
    public async Task OneCharactersAcceptanceCannotBeReusedForAnotherQuestWhileLegacyNullRowsRemainValid()
    {
        using var f = new Fixture(options); string id = Guid.NewGuid().ToString("N"); f.Seed((byte)QuestState.Accepted, id);
        await using (var context = f.Context())
        {
            context.CharacterQuest.Add(new() { Id = 42, QuestId = 9002, ContractAcceptanceId = id });
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
        await using var legacy = f.Context(); legacy.CharacterQuest.AddRange(new CharacterQuestModel { Id = 42, QuestId = 9003 }, new CharacterQuestModel { Id = 42, QuestId = 9004 });
        await legacy.SaveChangesAsync(); Assert.Equal(3, await legacy.CharacterQuest.CountAsync());
    }

    [Theory]
    [InlineData("")] [InlineData("00000000000000000000000000000000")] [InlineData("ABCDEF0123456789ABCDEF0123456789")]
    [InlineData("abc-def")] [InlineData("zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz")]
    public void MalformedStoredTokensAndSharedSnapshotsFailWithoutInventingAReplacement(string id)
    {
        using var f = new Fixture(options); var model = new CharacterQuestModel { Id = 42, QuestId = SourceFixture.QuestId, State = (byte)QuestState.Accepted, ContractAcceptanceId = id };
        Assert.Throws<InvalidOperationException>(() => new GameQuest(f.Inner.Source.Player, f.Info, model, globalQuestManager: f.Inner.Global, scriptManager: f.Inner.Scripts, gameTableManager: f.Inner.Source.Tables));
        Assert.Throws<ArgumentException>(() => new CharacterChallengeQuestSnapshot(new(42, SourceFixture.QuestId, 1, 0, null, null, id), []));
    }

    [Theory]
    [InlineData("ordinary")] [InlineData("unsupported")] [InlineData("custom-manager")]
    public async Task OrdinaryOrUnsupportedOrCustomManagerQuestsDoNotAcquirePointAuthority(string kind)
    {
        using var f = new Fixture(options);
        if (kind == "ordinary") { f.Info.Entry.Flags = 0; ((RecordingDispatchProxy<IQuestInfo>)(object)f.Info).SetMethodReturn(nameof(IQuestInfo.IsContract), false); }
        if (kind == "unsupported") f.Info.Entry.PeriodicQuestGroupId = 0;
        using var manager = f.Manager(custom: kind == "custom-manager"); manager.QuestAdd(f.Info); await f.Save(manager);
        Assert.Null(f.Active(manager).ContractAcceptanceId); Assert.Null(f.Read().ContractAcceptanceId); f.AssertNoAwards();
    }

    private sealed class CommitFault(bool after) : DbTransactionInterceptor
    {
        private int remaining = 1;
        private void Fail() { if (Interlocked.Exchange(ref remaining, 0) == 1) throw new IOException("Owned acceptance acknowledgement fault."); }
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction tx, TransactionEventData data, InterceptionResult result, CancellationToken cancellationToken = default)
        { if (!after) Fail(); return new(result); }
        public override Task TransactionCommittedAsync(DbTransaction tx, TransactionEndEventData data, CancellationToken cancellationToken = default)
        { if (after) Fail(); return Task.CompletedTask; }
    }

    private sealed class Fixture : IDisposable
    {
        internal readonly SourceFixture Inner;
        internal IQuestInfo Info => Inner.Info;
        internal Fixture(DbContextOptions<CharacterContext> options)
        {
            Inner = new(options); var entry = Info.Entry; entry.Flags = 0x80000; entry.PeriodicQuestGroupId = 52; entry.QuestRepeatPeriodEnum = (uint)QuestRepeatPeriod.Daily;
            entry.Objectives = [90010, 0, 0, 0, 0, 0]; entry.PushedItemIds = new uint[6]; entry.PushedItemCounts = new uint[6];
            var info = (RecordingDispatchProxy<IQuestInfo>)(object)Info; info.SetMethodReturn(nameof(IQuestInfo.IsContract), true);
            var reward = new Quest2RewardEntry { Id = 6867, Quest2Id = SourceFixture.QuestId, Quest2RewardTypeId = 3, ObjectId = 1, ObjectAmount = 1 };
            info.SetProperty(nameof(IQuestInfo.Rewards), ImmutableDictionary<uint, Quest2RewardEntry>.Empty.Add(reward.Id, reward));
            var tables = (RecordingDispatchProxy<IGameTableManager>)(object)Inner.Source.Tables;
            tables.SetProperty(nameof(IGameTableManager.Quest2), PlayerBindPointTests.Table(entry));
            tables.SetProperty(nameof(IGameTableManager.QuestObjective), PlayerBindPointTests.Table(Info.Objectives[0].Entry));
            tables.SetProperty(nameof(IGameTableManager.Quest2Reward), PlayerBindPointTests.Table(reward));
            tables.SetProperty(nameof(IGameTableManager.PeriodicQuestGroup), PlayerBindPointTests.Table(new PeriodicQuestGroupEntry { Id = 52, ContractTypeEnum = 2, ContractQualityEnum = 1, PeriodicQuestsOffered = 100, MaxPeriodicQuestsAllowed = 100 }));
            var global = (RecordingDispatchProxy<IGlobalQuestManager>)(object)Inner.Global;
            global.SetProperty(nameof(IGlobalQuestManager.NextDailyReset), DateTime.UtcNow.AddDays(1));
            global.SetMethodReturn(nameof(IGlobalQuestManager.GetQuestReceivers), Array.Empty<uint>()); global.SetMethodReturn(nameof(IGlobalQuestManager.GetQuestGivers), Array.Empty<uint>());
            global.SetMethodReturn(nameof(IGlobalQuestManager.GetQuestCommunicatorMessages), Array.Empty<ICommunicatorMessage>());
            Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.CurrencyManager), RecordingDispatchProxy<ICurrencyManager>.Create(out _));
            Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.AchievementManager), RecordingDispatchProxy<ICharacterAchievementManager>.Create(out _));
        }
        internal CharacterContext Context(params IInterceptor[] interceptors) => Inner.Context(interceptors);
        internal string Database { get { using var context = Context(); return Game.Entity.Item.GetRewardDatabaseKey(context); } }
        internal QuestManager Manager(bool load = false, bool custom = false)
        {
            var model = new CharacterModel { Id = 42 };
            if (load) { using var read = Context(); model.Quest.Add(read.CharacterQuest.AsNoTracking().Include(q => q.QuestObjective).Single()); }
            var manager = new QuestManager(Inner.Source.Player, model, globalQuestManager: Inner.Global, scriptManager: Inner.Scripts, gameTableManager: Inner.Source.Tables,
                contractManager: custom ? RecordingDispatchProxy<IContractManager>.Create(out _) : new ContractManager(Inner.Source.Tables));
            Inner.Source.PlayerProxy.SetProperty(nameof(IPlayer.QuestManager), manager); return manager;
        }
        internal GameQuest Active(QuestManager manager) => Assert.IsType<GameQuest>(Assert.Single(manager.GetActiveQuests()));
        internal GameQuest Any(QuestManager manager)
        {
            var method = typeof(QuestManager).GetMethod("GetQuest", BindingFlags.Instance | BindingFlags.NonPublic)!;
            return Assert.IsType<GameQuest>(method.Invoke(manager, [SourceFixture.QuestId, method.GetParameters()[1].DefaultValue]));
        }
        internal async Task Save(QuestManager manager) { await using var context = Context(); manager.Save(context); await context.SaveChangesAsync(); }
        internal CharacterQuestModel Read() { using var read = Context(); return read.CharacterQuest.AsNoTracking().Single(q => q.QuestId == SourceFixture.QuestId); }
        internal void Seed(byte state, string id)
        {
            Inner.Seed(); using var context = Context(); var row = context.CharacterQuest.Single(); row.State = state; row.ContractAcceptanceId = id; context.SaveChanges();
        }
        internal void End(QuestManager manager, string ending)
        {
            if (ending == "abandon") manager.QuestAbandon(SourceFixture.QuestId);
            else if (ending == "complete") { manager.QuestAchieve(SourceFixture.QuestId); manager.QuestComplete(SourceFixture.QuestId, 0, false); }
            else { Active(manager).State = QuestState.Botched; manager.Update(0); }
        }
        internal void AssertNoAwards()
        {
            using var context = Context(); Assert.Empty(context.CharacterChallengeRewardTrack); Assert.Empty(context.CharacterChallengeRewardClaim);
        }
        public void Dispose() => Inner.Dispose();
    }
}
