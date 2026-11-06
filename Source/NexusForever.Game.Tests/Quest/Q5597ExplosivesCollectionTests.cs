using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Quests.CrimsonIsle;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed class Q5597ExplosivesCollectionTests
{
    public interface IExplosiveEntity : ICreatureEntity, ICollectableUnitEntity { }
    private readonly IGameTableManager actualTables;
    public Q5597ExplosivesCollectionTests() { }
    private Q5597ExplosivesCollectionTests(IGameTableManager tables) => actualTables = tables;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SixDistinctExplosivesAchieveTheRealQuestWithoutRepeatedCredit(bool creature)
    {
        using var f = new PickupFixture(actualTables);
        for (int index = 0; index < 6; index++)
        {
            var pickup = f.Pickup(creature);
            pickup.Script.OnActivateSuccess(f.Actor.Player);
            pickup.Script.OnActivateSuccess(f.Actor.Player);
            Assert.Equal((uint)index + 1, f.Objective.Progress);
            Assert.Single(pickup.Owner.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        }
        Assert.Equal(QuestState.Achieved, f.Quest.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RefusedPendingDeletionDoesNotConsumeTheExplosiveAndCanRetry(bool creature)
    {
        using var f = new PickupFixture(actualTables);
        var pickup = f.Pickup(creature);
        f.Quest.EnqueueDelete(true);
        pickup.Script.OnActivateSuccess(f.Actor.Player);
        Assert.Equal(0u, f.Objective.Progress);
        Assert.Empty(pickup.Owner.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
        f.Quest.EnqueueDelete(false);
        pickup.Script.OnActivateSuccess(f.Actor.Player);
        Assert.Equal(1u, f.Objective.Progress);
        Assert.Single(pickup.Owner.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AlreadySatisfiedObjectiveDoesNotConsumeAnotherExplosive(bool creature)
    {
        using var f = new PickupFixture(actualTables);
        f.Objective.Progress = 6;
        var pickup = f.Pickup(creature);
        pickup.Script.OnActivateSuccess(f.Actor.Player);
        Assert.Equal(6u, f.Objective.Progress);
        Assert.Empty(pickup.Owner.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void FailedRemovalRetriesTheOriginalObjectWithoutAnotherCredit(bool creature)
    {
        using var f = new PickupFixture(actualTables);
        var pickup = f.Pickup(creature);
        int removals = 0;
        pickup.Owner.SetMethodHandler(nameof(IGridEntity.RemoveFromMap), _ =>
        {
            if (++removals == 1) throw new IOException("Owned explosive removal refusal.");
            return null;
        });
        Record.Exception(() => pickup.Script.OnActivateSuccess(f.Actor.Player));
        pickup.Script.OnActivateSuccess(f.Actor.Player);
        pickup.Script.OnActivateSuccess(f.Actor.Player);
        Assert.Equal(1u, f.Objective.Progress);
        Assert.Equal(2, removals);
    }

    [Fact]
    public void ReentrantObjectiveFeedbackCannotCollectTheSameExplosiveTwice()
    {
        using var f = new PickupFixture(actualTables);
        var pickup = f.Pickup(false);
        bool reentered = false;
        f.Storage.Source.SessionProxy.SetMethodHandler("EnqueueMessageEncrypted", _ =>
        {
            if (!reentered) { reentered = true; pickup.Script.OnActivateSuccess(f.Actor.Player); }
            return null;
        });
        pickup.Script.OnActivateSuccess(f.Actor.Player);
        Assert.True(reentered);
        Assert.Equal(1u, f.Objective.Progress);
        Assert.Single(pickup.Owner.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public void LoadingAnotherOwnerStartsAnIndependentPickup()
    {
        using var f = new PickupFixture(actualTables);
        var first = f.Pickup(true);
        first.Script.OnActivateSuccess(f.Actor.Player);
        var next = f.Pickup(true);
        var nextOwner = (ICreatureEntity)(object)next.Owner;
        ((Q5597ChuaExplosivesCreatureEntityScript)first.Script).OnLoad(nextOwner);
        first.Script.OnActivateSuccess(f.Actor.Player);
        Assert.Equal(2u, f.Objective.Progress);
        Assert.Single(next.Owner.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    [Fact]
    public async Task CollectedProgressSurvivesFullPlayerSaveFailureRetryAndFreshQuestReload()
    {
        using var f = new PickupFixture(actualTables);
        for (int index = 0; index < 6; index++) f.Pickup(false).Script.OnActivateSuccess(f.Actor.Player);
        Assert.Equal(QuestState.Achieved, f.Quest.State);
        await Assert.ThrowsAsync<IOException>(() => f.Persist(true));
        using (var read = f.Storage.Context()) Assert.Equal(0u, read.CharacterQuestObjective.Single().Progress);
        await f.Persist();
        using var verify = f.Storage.Context();
        Assert.Equal((byte)QuestState.Achieved, verify.CharacterQuest.Single().State);
        Assert.Equal(6u, verify.CharacterQuestObjective.Single().Progress);
        using var fresh = f.Reload();
        var quest = Assert.Single(fresh.Quests.GetActiveQuests());
        Assert.Equal(QuestState.Achieved, quest.State);
        Assert.Equal(6u, Assert.Single(quest).Progress);
        var unused = f.Pickup(true);
        unused.Script.OnActivateSuccess(fresh.Player);
        Assert.Empty(unused.Owner.GetInvocations(nameof(IGridEntity.RemoveFromMap)));
    }

    internal static async Task RunActualCatalogAsync(string recoveryRoot, string outputDirectory)
    {
        recoveryRoot = Path.GetFullPath(recoveryRoot);
        outputDirectory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(outputDirectory)) throw new IOException("Choose a fresh evidence directory.");
        Directory.CreateDirectory(outputDirectory);
        var (manifest, files) = ContractActualCatalogQualification.VerifyInputs(recoveryRoot);
        var tables = await ContractActualCatalogQualification.LoadTablesAsync(recoveryRoot);
        await tables.Initialise(new GameTableLoader()
            .AddGameTable<Quest2Entry>().AddGameTable<QuestObjectiveEntry>()
            .AddGameTable<Quest2RewardEntry>().AddGameTable<TargetGroupEntry>().AddGameTable<Creature2Entry>());
        Assert.Equal(new uint[] { 8256, 0, 0, 0, 0, 0 }, tables.Quest2.GetEntry(5597).Objectives);
        var row = tables.QuestObjective.GetEntry(8256);
        Assert.Equal(32u, row.Type); Assert.Equal(4u, row.Flags);
        Assert.Equal(364u, row.Data); Assert.Equal(6u, row.Count);
        Assert.NotNull(tables.Creature2.GetEntry(24286));
        var test = new Q5597ExplosivesCollectionTests(tables);
        var cases = new List<(string Name, Func<Task> Run)>();
        foreach (bool creature in new[] { false, true })
        {
            cases.Add(($"SixDistinctExplosives/{creature}", () => { test.SixDistinctExplosivesAchieveTheRealQuestWithoutRepeatedCredit(creature); return Task.CompletedTask; }));
            cases.Add(($"RefusedDeletionRetry/{creature}", () => { test.RefusedPendingDeletionDoesNotConsumeTheExplosiveAndCanRetry(creature); return Task.CompletedTask; }));
            cases.Add(($"SatisfiedObjective/{creature}", () => { test.AlreadySatisfiedObjectiveDoesNotConsumeAnotherExplosive(creature); return Task.CompletedTask; }));
            cases.Add(($"RemovalRetry/{creature}", () => { test.FailedRemovalRetriesTheOriginalObjectWithoutAnotherCredit(creature); return Task.CompletedTask; }));
        }
        cases.Add(("ReentrantObjectiveFeedback", () => { test.ReentrantObjectiveFeedbackCannotCollectTheSameExplosiveTwice(); return Task.CompletedTask; }));
        cases.Add(("IndependentLoadedOwner", () => { test.LoadingAnotherOwnerStartsAnIndependentPickup(); return Task.CompletedTask; }));
        cases.Add(("FailedPlayerSaveRetryReload", test.CollectedProgressSurvivesFullPlayerSaveFailureRetryAndFreshQuestReload));
        string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
        var json = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(outputDirectory, "inputs.json"), JsonSerializer.Serialize(new
        {
            ManifestSha256 = Hash(manifest), Files = files,
            ReaderSha256 = Hash(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location)),
            RuntimeSha256 = Hash(File.ReadAllBytes(typeof(NexusForever.Game.Quest.Quest).Assembly.Location)),
            ScriptSha256 = Hash(File.ReadAllBytes(typeof(Q5597ChuaExplosivesEntityScript).Assembly.Location)),
            TestsSha256 = Hash(File.ReadAllBytes(typeof(Q5597ExplosivesCollectionTests).Assembly.Location))
        }, json));
        var passed = new List<string>();
        foreach (var item in cases)
        {
            try { await item.Run(); passed.Add(item.Name); Console.WriteLine("PASS " + item.Name); }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(outputDirectory, "failure.json"), JsonSerializer.Serialize(new { Case = item.Name, Passed = passed, Error = error.ToString() }, json));
                throw;
            }
        }
        File.WriteAllText(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(new
        {
            CompletedUtc = DateTime.UtcNow, Passed = passed.Count, Cases = passed,
            Limit = "Real script and Player/Quest SQLite save/reload with actual recovered QuestInfo. Accepted quest is prepared; proxy map/session, no public NPC acceptance/turnin, physical map/activation, provider or native/retail qualification."
        }, json));
    }

    internal sealed class PickupFixture : IDisposable
    {
        internal readonly Store Storage = new(null);
        internal readonly Actor Actor;
        internal readonly NexusForever.Game.Quest.Quest Quest;
        internal IQuestObjective Objective => Assert.Single(Quest);
        internal PickupFixture(IGameTableManager actual = null)
        {
            var proxy = (RecordingDispatchProxy<IGameTableManager>)(object)Storage.Source.Tables;
            Quest2Entry entry;
            if (actual == null)
            {
                entry = new Quest2Entry { Id = 5597, Objectives = [8256, 0, 0, 0, 0, 0], PushedItemIds = new uint[6], PushedItemCounts = new uint[6] };
                proxy.SetProperty(nameof(IGameTableManager.QuestObjective), NexusForever.Game.Tests.Transport.PlayerBindPointTests.Table(
                    new QuestObjectiveEntry { Id = 8256, Type = 32, Flags = 4, Data = 364, Count = 6 }));
            }
            else
            {
                foreach (var property in typeof(IGameTableManager).GetProperties())
                    if (property.GetValue(actual) is { } value) proxy.SetProperty(property.Name, value);
                entry = actual.Quest2.GetEntry(5597);
            }
            var info = new QuestInfo(entry, Storage.Global, Storage.Source.Tables);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)Storage.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestInfo), info);
            using (var seed = Storage.Context())
            {
                seed.CharacterQuest.Add(new() { Id = 42, QuestId = 5597, State = (byte)QuestState.Accepted,
                    QuestObjective = new List<CharacterQuestObjectiveModel> { new() { Id = 42, QuestId = 5597, Index = 0 } } });
                seed.SaveChanges();
            }
            using var read = Storage.Context();
            Actor = new(Storage, actorModel: read.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single());
            Quest = Assert.IsType<NexusForever.Game.Quest.Quest>(Assert.Single(Actor.Quests.GetActiveQuests()));
        }
        internal (Q5597ChuaExplosivesEntityScriptBase Script, RecordingDispatchProxy<IExplosiveEntity> Owner) Pickup(bool creature)
        {
            var owner = RecordingDispatchProxy<IExplosiveEntity>.Create(out var proxy);
            if (creature)
            {
                var script = new Q5597ChuaExplosivesCreatureEntityScript();
                script.OnLoad(owner);
                return (script, proxy);
            }
            else
            {
                var script = new Q5597ChuaExplosivesEntityScript();
                script.OnLoad(owner);
                return (script, proxy);
            }
        }
        internal Actor Reload()
        {
            using var read = Storage.Context();
            return new(Storage, actorModel: read.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single());
        }
        internal Task Persist(bool fail = false) => CharacterAtomicSave.Execute(() =>
        {
            var context = Storage.Context();
            if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned Q5597 Character flush refusal.");
            return context;
        }, 42, Actor.Player.Save);
        public void Dispose() { Actor.Dispose(); Storage.Dispose(); }
    }
}
