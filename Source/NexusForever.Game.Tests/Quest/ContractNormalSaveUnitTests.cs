using System.Data.Common;
using System.Collections.Immutable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Challenges.Reward;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Quest.Reward;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.Game.Tests.Transport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;
using NexusForever.Script;
using NexusForever.Script.Template.Collection;
using Fixture = NexusForever.Game.Tests.Quest.ContractCompletionRepositoryTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed class ContractNormalSaveUnitTests
{
    private readonly DbContextOptions<CharacterContext> options;
    public ContractNormalSaveUnitTests() { }
    internal ContractNormalSaveUnitTests(DbContextOptions<CharacterContext> options) => this.options = options;
    public static IEnumerable<object[]> ActualSources() => ContractCompletionRepositoryTests.ActualSources();

    [Theory]
    [MemberData(nameof(ActualSources))]
    public async Task EveryActualSourceCommitsItsOrdinaryRewardGraphWithOneReceiptAndPointIncrement(uint quest)
    {
        using var f = new Fixture(options, quest); SeedOrdinary(f); var command = f.Command(); ContractNormalSaveUnit unit = null;
        int callbacks = 0; int thread = Environment.CurrentManagedThreadId;
        Task save = CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            callbacks++; Assert.Equal(thread, Environment.CurrentManagedThreadId);
            Assert.Equal(42ul, context.AtomicCharacterId);
            unit = new(context, command, f.Catalog);
            EnrollOrdinary(context, command); var stage = unit.Enroll();
            Assert.Null(unit.TerminalCommitObserved); Assert.Equal(command.Points, stage.Track.Points);
        });
        Assert.Equal(1, callbacks); await save; Assert.True(unit.TerminalCommitObserved);
        using var read = f.Context(); Assert.Equal(125ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(9001u, read.Item.Single().ItemId); Assert.Equal(3u, read.Item.Single().StackCount);
        Assert.Equal(17u, read.Character.Single().TotalXp); Assert.Equal("Character reward repository", read.Character.Single().Name);
        Assert.Equal(command.Points, read.CharacterContractRewardTrack.Single().Points);
        Assert.Equal(command.AcceptanceId, read.CharacterContractCompletion.Single().AcceptanceId);
        Assert.True(CharacterChallengeQuestSnapshot.Same(command.Changes.Single().After, f.Read(command.Source.QuestId)));
        // No replay of ordinary rewards on retry: a new actor must reload the
        // already-completed Quest and receipt rather than run gameplay again.
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42,
            context => { _ = new ContractNormalSaveUnit(context, command, f.Catalog); EnrollOrdinary(context, command); }));
        using var final = f.Context(); Assert.Equal(125ul, final.CharacterCurrency.Single().Amount);
        Assert.Single(final.Item); Assert.Single(final.CharacterContractCompletion);
    }

    [Theory]
    [InlineData("root")] [InlineData("objective")] [InlineData("acceptance")] [InlineData("extra-objective")]
    public async Task MissingOrDifferentQuestEnrollmentCannotCommitOnlyTheOrdinaryRewardsAndReceipt(string missing)
    {
        using var f = new Fixture(options); SeedOrdinary(f); var command = f.Command();
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            var unit = new ContractNormalSaveUnit(context, command, f.Catalog);
            EnrollOrdinary(context, command);
            if (missing == "root") context.ChangeTracker.Entries<CharacterQuestModel>().Single().State = EntityState.Unchanged;
            else if (missing == "objective") context.ChangeTracker.Entries<CharacterQuestObjectiveModel>().First().Entity.Progress++;
            else if (missing == "acceptance") context.ChangeTracker.Entries<CharacterQuestModel>().Single().Entity.ContractAcceptanceId = Guid.NewGuid().ToString("N");
            else context.CharacterQuestObjective.Add(new() { Id = 42, QuestId = command.Source.QuestId, Index = 8, Progress = 1 });
            unit.Enroll();
        }));
        AssertBaseline(f);
    }

    [Theory]
    [InlineData("receipt-target")] [InlineData("receipt-detach")] [InlineData("receipt-mask")] [InlineData("track-revision")]
    [InlineData("track-target")] [InlineData("track-detach")] [InlineData("track-mask")] [InlineData("track-original")]
    public async Task ReceiptAndPointOwnershipStartsAtEnrollmentBeforeTheOrdinaryGraphIsSealed(string mode)
    {
        using var f = new Fixture(options); SeedOrdinary(f); var command = f.Command();
        if (mode == "track-original")
        {
            using var seed = f.Context(); seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = command.Source.TrackId, Revision = 3 }); seed.SaveChanges();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            var unit = new ContractNormalSaveUnit(context, command, f.Catalog); EnrollOrdinary(context, command); unit.Enroll();
            if (mode.StartsWith("receipt", StringComparison.Ordinal))
            {
                var entry = context.ChangeTracker.Entries<CharacterContractCompletionModel>().Single();
                if (mode.EndsWith("target", StringComparison.Ordinal)) entry.Entity.Points++;
                if (mode.EndsWith("detach", StringComparison.Ordinal)) entry.State = EntityState.Detached;
                if (mode.EndsWith("mask", StringComparison.Ordinal)) entry.State = EntityState.Unchanged;
            }
            else
            {
                var entry = context.ChangeTracker.Entries<CharacterContractRewardTrackModel>().Single();
                if (mode.EndsWith("target", StringComparison.Ordinal)) entry.Entity.Points++;
                if (mode.EndsWith("detach", StringComparison.Ordinal)) entry.State = EntityState.Detached;
                if (mode.EndsWith("mask", StringComparison.Ordinal)) entry.State = EntityState.Unchanged;
                if (mode.EndsWith("original", StringComparison.Ordinal)) entry.Property(row => row.Revision).OriginalValue++;
                if (mode.EndsWith("revision", StringComparison.Ordinal)) entry.Entity.Revision++;
            }
        }));
        AssertBaseline(f, existingTrack: mode == "track-original");
    }

    [Theory]
    [InlineData("currency-target")] [InlineData("currency-mask")] [InlineData("currency-original")]
    [InlineData("item-detach")] [InlineData("receipt")] [InlineData("track")] [InlineData("quest")]
    [InlineData("late-entity")] [InlineData("character-target")] [InlineData("delete-mask")]
    public async Task SealedNormalTargetsAndWriteMasksCannotBeAlteredBeforeFlush(string mode)
    {
        using var f = new Fixture(options); SeedOrdinary(f); SeedDeleted(f); var command = f.Command();
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            var unit = new ContractNormalSaveUnit(context, command, f.Catalog); EnrollOrdinary(context, command);
            context.CharacterDatacube.Remove(context.CharacterDatacube.Single()); unit.Enroll();
            context.SavingChanges += (_, _) =>
            {
                switch (mode)
                {
                    case "currency-target": context.ChangeTracker.Entries<CharacterCurrencyModel>().Single().Entity.Amount++; break;
                    case "currency-mask": context.ChangeTracker.Entries<CharacterCurrencyModel>().Single().Property(row => row.Amount).IsModified = false; break;
                    case "currency-original": context.ChangeTracker.Entries<CharacterCurrencyModel>().Single().Property(row => row.Amount).OriginalValue++; break;
                    case "item-detach": context.ChangeTracker.Entries<ItemModel>().Single().State = EntityState.Detached; break;
                    case "receipt": context.ChangeTracker.Entries<CharacterContractCompletionModel>().Single().Entity.Points++; break;
                    case "track": context.ChangeTracker.Entries<CharacterContractRewardTrackModel>().Single().Entity.Points++; break;
                    case "quest": context.ChangeTracker.Entries<CharacterQuestModel>().Single().Entity.Timer = 77; break;
                    case "late-entity": context.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 2, Amount = 1 }); break;
                    case "character-target": context.ChangeTracker.Entries<CharacterModel>().Single().Entity.TotalXp++; break;
                    case "delete-mask": context.ChangeTracker.Entries<CharacterDatacubeModel>().Single().State = EntityState.Unchanged; break;
                }
            };
        }));
        AssertBaseline(f); using var read = f.Context(); Assert.Single(read.CharacterDatacube);
    }

    [Theory]
    [InlineData("currency")] [InlineData("item")] [InlineData("character")] [InlineData("receipt")]
    [InlineData("track")] [InlineData("quest")] [InlineData("objective")] [InlineData("deleted")]
    public async Task RawSqlAfterFlushCannotCommitAPartialOrAlteredNormalGraph(string mode)
    {
        using var f = new Fixture(options); SeedOrdinary(f); SeedDeleted(f); var command = f.Command();
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            var unit = new ContractNormalSaveUnit(context, command, f.Catalog); EnrollOrdinary(context, command);
            context.CharacterDatacube.Remove(context.CharacterDatacube.Single()); unit.Enroll();
            context.SavedChanges += (_, _) => context.Database.ExecuteSqlRaw(mode switch
            {
                "currency" => "UPDATE character_currency SET amount=999 WHERE id=42",
                "item" => "UPDATE item SET stackCount=99 WHERE ownerId=42",
                "character" => "UPDATE `character` SET name='foreign hidden field' WHERE id=42",
                "receipt" => "UPDATE character_contract_completion SET points=999 WHERE id=42",
                "track" => "UPDATE character_contract_reward_track SET revision=99 WHERE id=42",
                "quest" => "UPDATE character_quest SET timer=999 WHERE id=42",
                "objective" => "UPDATE character_quest_objective SET progress=999 WHERE id=42",
                _ => "INSERT INTO character_datacube(id, datacube) VALUES(42, 7)"
            });
        }));
        AssertBaseline(f); using var read = f.Context(); Assert.Single(read.CharacterDatacube);
    }

    [Theory]
    [InlineData("flush")] [InlineData("before-commit")] [InlineData("lost-ack")]
    public async Task FaultsNeverReplayActorEnrollmentAndANegativeCommitObservationDoesNotProveRollback(string mode)
    {
        using var f = new Fixture(options); SeedOrdinary(f); var command = f.Command(); int calls = 0; ContractNormalSaveUnit unit = null;
        await Assert.ThrowsAsync<IOException>(() => CharacterAtomicSave.Execute(() => f.Context(new SaveFault(mode), new CommitFault(mode)), 42, context =>
        { calls++; unit = new(context, command, f.Catalog); EnrollOrdinary(context, command); unit.Enroll(); }));
        Assert.Equal(1, calls); Assert.False(unit.TerminalCommitObserved);
        if (mode == "lost-ack")
        {
            using var read = f.Context(); Assert.Single(read.CharacterContractCompletion); Assert.Equal(125ul, read.CharacterCurrency.Single().Amount); Assert.Single(read.Item);
        }
        else AssertBaseline(f);
    }

    [Theory]
    [InlineData("unowned")] [InlineData("late-begin")] [InlineData("never-enrolled")]
    [InlineData("early-flush")] [InlineData("early-commit")] [InlineData("second-enrollment")]
    public async Task UnitRequiresTheAtomicContextAndExactlyOneCompleteEnrollment(string mode)
    {
        using var f = new Fixture(options); SeedOrdinary(f); var command = f.Command();
        if (mode == "unowned")
        {
            using var unowned = f.Context(); Assert.Throws<InvalidOperationException>(() => new ContractNormalSaveUnit(unowned, command, f.Catalog));
            Assert.True(unowned.IsSaveRejected); AssertBaseline(f); return;
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        {
            if (mode == "late-begin") context.CharacterCurrency.Single();
            var unit = new ContractNormalSaveUnit(context, command, f.Catalog); EnrollOrdinary(context, command);
            if (mode == "never-enrolled") return;
            unit.Enroll();
            if (mode == "early-flush") context.SaveChanges();
            if (mode == "early-commit") context.Database.CurrentTransaction!.Commit();
            if (mode == "second-enrollment") unit.Enroll();
        }));
        AssertBaseline(f);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task TrackOverflowRollsBackAllOrdinaryEnrollment(bool revision)
    {
        using var f = new Fixture(options); SeedOrdinary(f); var command = f.Command();
        using (var seed = f.Context())
        {
            seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = command.Source.TrackId, CycleId = 7, Active = true,
                Points = revision ? 9u : uint.MaxValue, Revision = revision ? ulong.MaxValue : 3ul }); seed.SaveChanges();
        }
        await Assert.ThrowsAsync<OverflowException>(() => CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        { var unit = new ContractNormalSaveUnit(context, command, f.Catalog); EnrollOrdinary(context, command); unit.Enroll(); }));
        using var read = f.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Equal(100ul, read.CharacterCurrency.Single().Amount);
        Assert.Empty(read.Item); Assert.Equal(7ul, read.CharacterContractRewardTrack.Single().CycleId); Assert.True(read.CharacterContractRewardTrack.Single().Active);
    }

    [Fact]
    public async Task ExistingTrackCycleAndActiveSelectionSurviveOrdinaryCompletion()
    {
        using var f = new Fixture(options); SeedOrdinary(f); var command = f.Command();
        using (var seed = f.Context())
        { seed.CharacterContractRewardTrack.Add(new() { Id = 42, TrackId = command.Source.TrackId, CycleId = 7, Active = true, Points = 9, Revision = 3 }); seed.SaveChanges(); }
        await CharacterAtomicSave.Execute(() => f.Context(), 42, context =>
        { var unit = new ContractNormalSaveUnit(context, command, f.Catalog); EnrollOrdinary(context, command); unit.Enroll(); });
        using var read = f.Context(); var track = read.CharacterContractRewardTrack.Single();
        Assert.Equal(7ul, track.CycleId); Assert.True(track.Active); Assert.Equal(9 + command.Points, track.Points); Assert.Equal(4ul, track.Revision);
    }

    [Theory]
    [InlineData("success")] [InlineData("later-intent")] [InlineData("rollback")]
    public async Task RealQuestFrozenEnrollmentAndOrdinaryCurrencySaveShareTheUnitWithoutLosingLaterQuestIntent(string mode)
    {
        using var f = new Fixture(options); SeedOrdinary(f); var before = f.Read(f.Source.QuestId);
        var player = RecordingDispatchProxy<IPlayer>.Create(out var pp); pp.SetProperty(nameof(IPlayer.CharacterId), 42ul);
        pp.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        var info = RecordingDispatchProxy<IQuestInfo>.Create(out var ip); ip.SetProperty(nameof(IQuestInfo.Entry), new Quest2Entry { Id = f.Source.QuestId });
        ip.SetMethodReturn(nameof(IQuestInfo.IsContract), true);
        ip.SetProperty(nameof(IQuestInfo.Objectives), ImmutableList.Create<IQuestObjectiveInfo>(
            new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90010, Count = 10 }), new QuestObjectiveInfo(new QuestObjectiveEntry { Id = 90011, Count = 10 })));
        var scripts = RecordingDispatchProxy<IScriptManager>.Create(out var sp);
        sp.SetMethodReturn(nameof(IScriptManager.InitialiseOwnedScripts), RecordingDispatchProxy<IScriptCollection>.Create(out _));
        var global = RecordingDispatchProxy<IGlobalQuestManager>.Create(out var gp);
        gp.SetMethodReturn(nameof(IGlobalQuestManager.GetQuestCommunicatorQuestStateTriggers), Array.Empty<ICommunicatorMessage>());
        CharacterQuestModel model;
        using (var read = f.Context()) model = read.CharacterQuest.AsNoTracking().Include(row => row.QuestObjective).Single();
        var quest = new Game.Quest.Quest(player, info, model, globalQuestManager: global, scriptManager: scripts);
        quest.State = QuestState.Completed; quest.First().Progress = 7;
        quest.Reset = new DateTime(2026, 10, 31, 12, 34, 56, DateTimeKind.Utc).AddTicks(9876543);
        var token = new object();
        using var identity = f.Context();
        using var held = QuestSaveScope.AcquireFrozen(quest, token, NexusForever.Game.Entity.Item.GetRewardDatabaseKey(identity), 42);
        var graph = quest.CaptureNormalSave(token); var after = quest.CaptureRewardGraph(token).Snapshot;
        var command = new ContractCompletionCommand(42, f.Token, f.Source, f.Source.BasePoints, "real-normal-enrollment", 0, DateTime.UtcNow, [new(before, after)]);
        var tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tp);
        tp.SetProperty(nameof(IGameTableManager.CurrencyType), PlayerBindPointTests.Table(new CurrencyTypeEntry { Id = 1 }));
        var currency = new Currency(new CharacterCurrencyModel { Id = 42, CurrencyId = 1, Amount = 100 }, tables) { Amount = 125 };
        var currencySave = currency.CaptureNormalSave(token);
        if (mode == "later-intent") quest.Timer = 901;
        Task Save() => CharacterAtomicSave.Execute(() => mode == "rollback" ? f.Context(new SaveFault("flush")) : f.Context(), 42, context =>
        {
            var unit = new ContractNormalSaveUnit(context, command, f.Catalog);
            EnrollOrdinary(context, command, questGraph: false, wallet: false); currencySave(context); graph.Stage(context); unit.Enroll();
        });
        if (mode == "rollback") { await Assert.ThrowsAsync<IOException>(Save); AssertBaseline(f); Assert.True(quest.NeedsPersistenceSave); return; }
        await Save(); held.Dispose(); using (var read = f.Context()) Assert.Equal(125ul, read.CharacterCurrency.Single().Amount);
        Assert.Equal(before.Quest.Timer, f.Read(f.Source.QuestId).Quest.Timer); Assert.Equal(7u, f.Read(f.Source.QuestId).Objectives[0].Progress);
        if (mode == "later-intent")
        {
            Assert.True(quest.NeedsPersistenceSave);
            using var later = f.Context(); quest.Save(later); await later.SaveChangesAsync();
            Assert.Equal(901u, f.Read(f.Source.QuestId).Quest.Timer); Assert.Single(later.CharacterContractCompletion);
        }
        Assert.False(quest.NeedsPersistenceSave);
    }

    internal static void SeedOrdinary(Fixture f)
    {
        using var seed = f.Context(); seed.CharacterCurrency.Add(new() { Id = 42, CurrencyId = 1, Amount = 100 }); seed.SaveChanges();
    }
    private static void SeedDeleted(Fixture f)
    { using var seed = f.Context(); seed.CharacterDatacube.Add(new() { Id = 42, Datacube = 7 }); seed.SaveChanges(); }
    internal static void EnrollOrdinary(CharacterContext context, ContractCompletionCommand command, bool questGraph = true, bool wallet = true)
    {
        // Ordinary managers often enroll partial stubs. Their untouched CLR
        // defaults must not become either SQL writes or expected SQL values.
        var character = new CharacterModel { Id = 42, TotalXp = 17 }; context.Attach(character); context.Entry(character).Property(row => row.TotalXp).IsModified = true;
        if (wallet)
        {
            var currency = new CharacterCurrencyModel { Id = 42, CurrencyId = 1, Amount = 125 }; context.Attach(currency); context.Entry(currency).Property(row => row.Amount).IsModified = true;
        }
        context.Item.Add(new() { Id = 900, OwnerId = 42, ItemId = 9001, StackCount = 3 });
        if (!questGraph) return;
        foreach (var change in command.Changes)
        {
            var root = change.After.Quest.ToModel(); context.Attach(root); context.Entry(root).Property(row => row.State).IsModified = true;
            foreach (var leaf in change.After.Objectives) context.Attach(leaf.ToModel());
        }
    }
    private static void AssertBaseline(Fixture f, bool existingTrack = false)
    {
        using var read = f.Context(); Assert.Empty(read.CharacterContractCompletion); Assert.Empty(read.Item);
        if (existingTrack) { var track = Assert.Single(read.CharacterContractRewardTrack); Assert.Equal(0u, track.Points); Assert.Equal(3ul, track.Revision); }
        else Assert.Empty(read.CharacterContractRewardTrack);
        Assert.Equal(100ul, read.CharacterCurrency.Single().Amount); Assert.Equal(0u, read.Character.Single().TotalXp);
        Assert.Equal("Character reward repository", read.Character.Single().Name); Assert.Equal((byte)QuestState.Achieved, read.CharacterQuest.Single().State);
    }
    private sealed class SaveFault(string mode) : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
            => mode == "flush" ? throw new IOException("Normal Character flush fault.") : new(result);
    }
    private sealed class CommitFault(string mode) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
            => mode == "before-commit" ? throw new IOException("Normal Character commit fault.") : new(result);
        public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData, CancellationToken cancellationToken = default)
            => mode == "lost-ack" ? Task.FromException(new IOException("Normal Character commit acknowledgement lost.")) : Task.CompletedTask;
    }
}
