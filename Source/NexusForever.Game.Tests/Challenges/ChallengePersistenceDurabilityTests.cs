using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Challenges;
using NexusForever.Game.Static.Challenges;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Network.Session;

namespace NexusForever.Game.Tests.Challenges;

public sealed class ChallengePersistenceDurabilityTests : IDisposable
{
    private const ulong OwnerId = 9581;
    private const ushort ChallengeId = 1001;
    private readonly string directory = Path.Combine(Path.GetTempPath(), "NexusForever-ChallengePersistence-" + Guid.NewGuid().ToString("N"));
    private readonly DbContextOptions<CharacterContext> options;

    public ChallengePersistenceDurabilityTests()
    {
        Directory.CreateDirectory(directory);
        // A unique real SQLite file allows fresh-context reloads without global pools.
        // The synthetic owner has no unrelated character graph; FK checks are disabled.
        options = new DbContextOptionsBuilder<CharacterContext>()
            .UseSqlite($"Data Source={Path.Combine(directory, "challenge.sqlite")};Foreign Keys=False;Pooling=False")
            .Options;
        using var context = Context();
        context.Database.EnsureCreated();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActivatedChallengeRollbackOrTransactionDisposalRetainsCreate(bool disposeTransaction)
    {
        ChallengeManager manager = Manager();
        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        await using (CharacterContext context = Context())
        {
            var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            await context.SaveChangesAsync();
            if (disposeTransaction) await transaction.DisposeAsync();
            else
            {
                await transaction.RollbackAsync();
                await transaction.DisposeAsync();
            }
        }

        Assert.Null(await Read());
        await Save(manager);
        CharacterChallengeModel row = Assert.IsType<CharacterChallengeModel>(await Read());
        Assert.True(row.Activated);
        Assert.Equal(300d, row.ActiveTimerSeconds);
    }

    [Fact]
    public async Task LoadedProgressRollbackRetriesWithoutLosingExistingColumns()
    {
        CharacterChallengeModel original = await Seed();
        ChallengeManager manager = Manager(original);
        Assert.True(manager.TryAdvanceProgress(ChallengeId));
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        Assert.Equal(4u, (await Read())?.CurrentCount);
        await Save(manager);
        CharacterChallengeModel row = Assert.IsType<CharacterChallengeModel>(await Read());
        Assert.Equal(5u, row.CurrentCount);
        Assert.Equal(original.CompletionCount, row.CompletionCount);
        Assert.Equal(original.LastRewardTier, row.LastRewardTier);
        Assert.Equal(original.ActiveTimerSeconds, row.ActiveTimerSeconds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedImplicitSqlRetainsCreateOrUpdateForFreshRetry(bool loaded)
    {
        ChallengeManager manager = loaded ? Manager(await Seed()) : Manager();
        if (loaded) Assert.True(manager.TryAdvanceProgress(ChallengeId));
        else manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);

        await using (CharacterContext context = Context())
        {
            string rejectSql = loaded
                ? "CREATE TRIGGER reject_challenge BEFORE UPDATE ON character_challenge BEGIN SELECT RAISE(ABORT, 'owned challenge failure'); END"
                : "CREATE TRIGGER reject_challenge BEFORE INSERT ON character_challenge BEGIN SELECT RAISE(ABORT, 'owned challenge failure'); END";
            await context.Database.ExecuteSqlRawAsync(rejectSql);
            manager.Save(context);
            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER reject_challenge");
        }

        await Save(manager);
        CharacterChallengeModel row = Assert.IsType<CharacterChallengeModel>(await Read());
        Assert.True(row.Activated);
        Assert.Equal(loaded ? 5u : 0u, row.CurrentCount);
    }

    [Fact]
    public async Task ExplicitCommitWithoutSqlDoesNotAcknowledgeStagedCreate()
    {
        ChallengeManager manager = Manager();
        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            await transaction.CommitAsync();
        }

        Assert.Null(await Read());
        await Save(manager);
        Assert.True((await Read())?.Activated);
    }

    [Fact]
    public async Task SameContextRestageBeforeSqlReusesOneRowAndLatestProgress()
    {
        ChallengeManager manager = Manager();
        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        await using (CharacterContext context = Context())
        {
            manager.Save(context);
            Assert.True(manager.TryAdvanceProgress(ChallengeId, 3));
            manager.Save(context);
            Assert.Single(context.ChangeTracker.Entries<CharacterChallengeModel>());
            await context.SaveChangesAsync();
        }

        Assert.Equal(3u, (await Read())?.CurrentCount);
    }

    [Fact]
    public async Task DifferentContextCannotSilentlyOmitAnActiveOwnersChallenge()
    {
        ChallengeManager manager = Manager();
        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        await using (CharacterContext owning = Context())
        {
            manager.Save(owning);
            await using (CharacterContext contender = Context())
                Assert.ThrowsAny<InvalidOperationException>(() => manager.Save(contender));
            await owning.SaveChangesAsync();
        }

        Assert.True((await Read())?.Activated);
    }

    [Fact]
    public async Task TimerMutationAfterSqlIsNotConsumedByEarlierCommit()
    {
        ChallengeManager manager = Manager(await Seed());
        Assert.True(manager.TryAdvanceProgress(ChallengeId));
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            await context.SaveChangesAsync();
            // A still-running timer does not call MarkDirty in Update; snapshot
            // comparison must retain this later state without changing timer policy.
            manager.Update(12.5);
            await transaction.CommitAsync();
        }

        Assert.Equal(240d, (await Read())?.ActiveTimerSeconds);
        await Save(manager);
        CharacterChallengeModel row = Assert.IsType<CharacterChallengeModel>(await Read());
        Assert.Equal(227.5d, row.ActiveTimerSeconds);
        Assert.Equal(5u, row.CurrentCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavepointRollbackThenCommitReconcilesCreateInFreshContext(bool afterWrites)
    {
        ChallengeManager manager = Manager();
        manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            if (!afterWrites) await transaction.CreateSavepointAsync("challenge_write");
            manager.Save(context);
            await context.SaveChangesAsync();
            if (afterWrites) await transaction.CreateSavepointAsync("challenge_write");
            await transaction.RollbackToSavepointAsync("challenge_write");
            await transaction.CommitAsync();
        }

        Assert.Equal(afterWrites, await Exists());
        await Save(manager);
        Assert.True((await Read())?.Activated);
        await using var reload = Context();
        Assert.Single(await reload.CharacterChallenge.ToListAsync());
    }

    [Fact]
    public async Task SuccessfulLoadedSavePreservesAnUnrecognizedExistingChallengeRow()
    {
        CharacterChallengeModel original = await Seed();
        var untouched = new CharacterChallengeModel
        {
            Id = OwnerId, ChallengeId = 1002, CompletionCount = 29,
            CurrentCount = 37, LastRewardTier = 2, CooldownTimerSeconds = 76.5
        };
        await using (CharacterContext context = Context())
        {
            context.CharacterChallenge.Add(untouched);
            await context.SaveChangesAsync();
        }
        ChallengeManager manager = Manager(original, untouched);
        Assert.True(manager.TryAdvanceProgress(ChallengeId));

        await Save(manager);

        Assert.Equal(5u, (await Read())?.CurrentCount);
        await using var reload = Context();
        CharacterChallengeModel retained = await reload.CharacterChallenge.SingleAsync(row => row.ChallengeId == 1002);
        Assert.Equal(untouched.CompletionCount, retained.CompletionCount);
        Assert.Equal(untouched.CurrentCount, retained.CurrentCount);
        Assert.Equal(untouched.LastRewardTier, retained.LastRewardTier);
        Assert.Equal(untouched.CooldownTimerSeconds, retained.CooldownTimerSeconds);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AbandonedContextBeforeSqlRetainsCreateOrLoadedUpdate(bool loaded)
    {
        ChallengeManager manager = loaded ? Manager(await Seed()) : Manager();
        if (loaded) Assert.True(manager.TryAdvanceProgress(ChallengeId));
        else manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        await using (CharacterContext abandoned = Context()) manager.Save(abandoned);

        Assert.Equal(loaded, await Exists());
        await Save(manager);
        CharacterChallengeModel row = Assert.IsType<CharacterChallengeModel>(await Read());
        Assert.Equal(loaded ? 5u : 0u, row.CurrentCount);
        Assert.True(row.Activated);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActiveContextSavepointRollbackRejectsRestagingThenFreshUnitConverges(bool loaded)
    {
        ChallengeManager manager = loaded ? Manager(await Seed()) : Manager();
        if (loaded) Assert.True(manager.TryAdvanceProgress(ChallengeId));
        else manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            await transaction.CreateSavepointAsync("before_challenge");
            manager.Save(context);
            await context.SaveChangesAsync();
            await transaction.RollbackToSavepointAsync("before_challenge");
            Assert.ThrowsAny<InvalidOperationException>(() => manager.Save(context));
            await transaction.CommitAsync();
        }

        if (loaded) Assert.Equal(4u, (await Read())?.CurrentCount);
        else Assert.Null(await Read());
        await Save(manager);
        Assert.Equal(loaded ? 5u : 0u, (await Read())?.CurrentCount);
    }

    [Fact]
    public async Task FlushedProgressThenUnflushedRestageAcknowledgesOnlyItsSqlSnapshot()
    {
        ChallengeManager manager = Manager(await Seed());
        Assert.True(manager.TryAdvanceProgress(ChallengeId));
        await using (CharacterContext context = Context())
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            manager.Save(context);
            await context.SaveChangesAsync();
            Assert.True(manager.TryAdvanceProgress(ChallengeId, 3));
            manager.Save(context);
            await transaction.CommitAsync();
        }

        Assert.Equal(5u, (await Read())?.CurrentCount);
        await Save(manager);
        Assert.Equal(8u, (await Read())?.CurrentCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DuplicateManagerOwnerCannotStageTheSameCharacterWhileItsLeaseIsActive(bool sameContext)
    {
        CharacterChallengeModel original = await Seed();
        ChallengeManager first = Manager(original);
        ChallengeManager second = Manager(original);
        Assert.True(first.TryAdvanceProgress(ChallengeId));
        Assert.True(second.TryAdvanceProgress(ChallengeId, 2));
        await using (CharacterContext owner = Context())
        {
            first.Save(owner);
            if (sameContext) Assert.ThrowsAny<InvalidOperationException>(() => second.Save(owner));
            else
            {
                await using CharacterContext contender = Context();
                Assert.ThrowsAny<InvalidOperationException>(() => second.Save(contender));
            }
            await owner.SaveChangesAsync();
        }

        Assert.Equal(5u, (await Read())?.CurrentCount);
        // The released lease does not authorize the stale manager's old snapshot.
        await using CharacterContext stale = Context();
        Assert.ThrowsAny<InvalidOperationException>(() => second.Save(stale));
        Assert.Equal(5u, (await Read())?.CurrentCount);
    }

    [Fact]
    public async Task SequentialStaleOwnerCannotOverwriteAnotherManagersCommittedProgress()
    {
        CharacterChallengeModel original = await Seed();
        ChallengeManager current = Manager(original);
        ChallengeManager stale = Manager(original);
        Assert.True(current.TryAdvanceProgress(ChallengeId));
        await Save(current);
        Assert.True(stale.TryAdvanceProgress(ChallengeId, 2));
        await using (CharacterContext rejected = Context())
            Assert.ThrowsAny<InvalidOperationException>(() => stale.Save(rejected));

        ChallengeManager fresh = Manager(Assert.IsType<CharacterChallengeModel>(await Read()));
        Assert.True(fresh.TryAdvanceProgress(ChallengeId));
        await Save(fresh);
        Assert.Equal(6u, (await Read())?.CurrentCount);
    }

    [Fact]
    public async Task SharedContextReusesItsFullyLoadedChallengeModel()
    {
        ChallengeManager manager = Manager(await Seed());
        Assert.True(manager.TryAdvanceProgress(ChallengeId));
        await using (CharacterContext context = Context())
        {
            CharacterChallengeModel tracked = await context.CharacterChallenge.SingleAsync();
            manager.Save(context);
            Assert.Same(tracked, Assert.Single(context.ChangeTracker.Entries<CharacterChallengeModel>()).Entity);
            await context.SaveChangesAsync();
        }

        Assert.Equal(5u, (await Read())?.CurrentCount);
    }

    [Fact]
    public async Task SharedContextRejectsAnIncompleteChallengeStubAndReleasesOnDisposal()
    {
        ChallengeManager manager = Manager(await Seed());
        Assert.True(manager.TryAdvanceProgress(ChallengeId));
        await using (CharacterContext incomplete = Context())
        {
            incomplete.Attach(new CharacterChallengeModel { Id = OwnerId, ChallengeId = ChallengeId });
            Assert.ThrowsAny<InvalidOperationException>(() => manager.Save(incomplete));
        }

        await Save(manager);
        Assert.Equal(5u, (await Read())?.CurrentCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SaveWithoutAcceptingTrackedChangesStillAcknowledgesTheActualSql(bool loaded)
    {
        ChallengeManager manager = loaded ? Manager(await Seed()) : Manager();
        if (loaded) Assert.True(manager.TryAdvanceProgress(ChallengeId));
        else manager.HandleChoice(ChallengeId, ChallengeChoice.Activate);
        await using (CharacterContext context = Context())
        {
            manager.Save(context);
            await context.SaveChangesAsync(acceptAllChangesOnSuccess: false);
        }

        Assert.Equal(loaded ? 5u : 0u, (await Read())?.CurrentCount);
        Assert.True(manager.TryAdvanceProgress(ChallengeId));
        await Save(manager);
        Assert.Equal(loaded ? 6u : 1u, (await Read())?.CurrentCount);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("cooldown")]
    [InlineData("area")]
    public async Task NonterminalTimerOnlyChangesPersistWithoutAProgressProducer(string timer)
    {
        CharacterChallengeModel original = await Seed(configure: row =>
        {
            row.Activated = timer != "cooldown";
            row.OnCooldown = timer == "cooldown";
            row.LeftArea = timer == "area";
        });
        ChallengeManager manager = Manager(original);
        manager.Update(0.5);

        await Save(manager);

        CharacterChallengeModel row = Assert.IsType<CharacterChallengeModel>(await Read());
        Assert.Equal(timer == "cooldown" ? 19d : 19.5d, row.CooldownTimerSeconds);
        Assert.Equal(timer == "area" ? 3d : 3.5d, row.AreaFailTimerSeconds);
        Assert.Equal(timer == "cooldown" ? 240d : 239.5d, row.ActiveTimerSeconds);
        Assert.Equal(original.CompletionCount, row.CompletionCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("0123456789abcdef0123456789abcdef")]
    public async Task OrdinarySaveRoundTripsLoadedAttemptMarkerWithoutBackfillingLegacyRows(string attempt)
    {
        ChallengeManager manager = Manager(await Seed(attempt));
        manager.Update(0.5);
        await Save(manager);
        Assert.Equal(attempt, (await Read())?.ActiveAttemptId);

        ChallengeManager reloaded = Manager(Assert.IsType<CharacterChallengeModel>(await Read()));
        reloaded.Update(0.5);
        await Save(reloaded);
        Assert.Equal(attempt, (await Read())?.ActiveAttemptId);
        Assert.Equal(239d, (await Read())?.ActiveTimerSeconds);
    }

    private CharacterContext Context() => new(options);

    private async Task Save(ChallengeManager manager)
    {
        await using CharacterContext context = Context();
        manager.Save(context);
        await context.SaveChangesAsync();
    }

    private async Task<CharacterChallengeModel> Read()
    {
        await using CharacterContext context = Context();
        return await context.CharacterChallenge.AsNoTracking().SingleOrDefaultAsync(row => row.Id == OwnerId && row.ChallengeId == ChallengeId);
    }

    private async Task<bool> Exists() => await Read() != null;

    private async Task<CharacterChallengeModel> Seed(string attempt = null, Action<CharacterChallengeModel> configure = null)
    {
        var row = new CharacterChallengeModel
        {
            Id = OwnerId, ChallengeId = ChallengeId, ActiveAttemptId = attempt, Activated = true,
            CurrentCount = 4, CompletionCount = 11, LastRewardTier = 2,
            ActiveTimerSeconds = 240, CooldownTimerSeconds = 19.5, AreaFailTimerSeconds = 3.5
        };
        configure?.Invoke(row);
        await using CharacterContext context = Context();
        context.CharacterChallenge.Add(row);
        await context.SaveChangesAsync();
        return row;
    }

    private static ChallengeManager Manager(params CharacterChallengeModel[] loaded)
    {
        IGameTableManager tables = RecordingDispatchProxy<IGameTableManager>.Create(out var tableProxy);
        tableProxy.SetProperty(nameof(IGameTableManager.Challenge), Table(new ChallengeEntry
        {
            Id = ChallengeId, ChallengeTypeEnum = (uint)ChallengeType.General,
            ChallengeFlags = 0x10, ChallengeTierId00 = 2001, TargetGroupIdRewardPane = 42
        }));
        tableProxy.SetProperty(nameof(IGameTableManager.ChallengeTier), Table(new ChallengeTierEntry { Id = 2001, Count = 100 }));
        IPlayer player = RecordingDispatchProxy<IPlayer>.Create(out var playerProxy);
        playerProxy.SetProperty(nameof(IPlayer.CharacterId), OwnerId);
        playerProxy.SetProperty(nameof(IPlayer.Session), RecordingDispatchProxy<IGameSession>.Create(out _));
        var model = new CharacterModel { Id = OwnerId };
        foreach (CharacterChallengeModel row in loaded) model.Challenge.Add(row);
        var manager = new ChallengeManager(player, model, tables);
        playerProxy.SetProperty(nameof(IPlayer.ChallengeManager), manager);
        return manager;
    }

    private static GameTable<T> Table<T>(T entry) where T : class, new()
    {
        var table = (GameTable<T>)RuntimeHelpers.GetUninitializedObject(typeof(GameTable<T>));
        uint id = (uint)typeof(T).GetField("Id")!.GetValue(entry)!;
        typeof(GameTable<T>).GetField("<Entries>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new[] { entry });
        typeof(GameTable<T>).GetField("header", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, new GameTableHeader { MaxId = id + 1 });
        int[] lookup = Enumerable.Repeat(-1, checked((int)id + 1)).ToArray();
        lookup[id] = 0;
        typeof(GameTable<T>).GetField("lookup", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(table, lookup);
        return table;
    }

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }
}
