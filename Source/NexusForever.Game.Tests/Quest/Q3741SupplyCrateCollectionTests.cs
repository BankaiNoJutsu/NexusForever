using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Configuration.Model;
using NexusForever.GameTable.Model;
using NexusForever.Script.Main.Quests.NorthernWilds;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed class Q3741SupplyCrateCollectionTests
{
    public interface ISupplyCrateEntity : ICreatureEntity, ICollectableUnitEntity { }
    private readonly IGameTableManager actualTables;
    public Q3741SupplyCrateCollectionTests() { }
    private Q3741SupplyCrateCollectionTests(IGameTableManager tables) => actualTables = tables;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SixDistinctSupplyCratesAchieveTheRealQuestWithoutRepeatedCredit(bool creature)
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
    public void RefusedPendingDeletionDoesNotConsumeTheSupplyCrateAndCanRetry(bool creature)
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
    public void AlreadySatisfiedObjectiveDoesNotConsumeAnotherSupplyCrate(bool creature)
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
            if (++removals == 1) throw new IOException("Owned supply crate removal refusal.");
            return null;
        });
        Record.Exception(() => pickup.Script.OnActivateSuccess(f.Actor.Player));
        pickup.Script.OnActivateSuccess(f.Actor.Player);
        pickup.Script.OnActivateSuccess(f.Actor.Player);
        Assert.Equal(1u, f.Objective.Progress);
        Assert.Equal(2, removals);
    }

    [Fact]
    public void ReentrantObjectiveFeedbackCannotCollectTheSameSupplyCrateTwice()
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
        ((Q3741ExileSupplyCrateEntityScript)first.Script).OnLoad(nextOwner);
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
        byte[] manifest = File.ReadAllBytes(Path.Combine(recoveryRoot, ".nexusforever-assets.json"));
        using var inventory = JsonDocument.Parse(manifest);
        var loader = new GameTableLoader()
            .AddGameTable<RewardTrackEntry>().AddGameTable<RewardTrackRewardsEntry>()
            .AddGameTable<Item2Entry>().AddGameTable<AccountItemEntry>()
            .AddGameTable<CurrencyTypeEntry>().AddGameTable<AccountCurrencyTypeEntry>()
            .AddGameTable<Item2FamilyEntry>().AddGameTable<Item2CategoryEntry>().AddGameTable<Item2TypeEntry>()
            .AddGameTable<ItemSlotEntry>().AddGameTable<ItemBudgetEntry>().AddGameTable<ItemStatEntry>()
            .AddGameTable<ItemQualityEntry>().AddGameTable<ItemRandomStatEntry>()
            .AddGameTable<GameFormulaEntry>().AddGameTable<UnitProperty2Entry>()
            .AddGameTable<Quest2Entry>().AddGameTable<QuestObjectiveEntry>()
            .AddGameTable<Quest2RewardEntry>().AddGameTable<TargetGroupEntry>().AddGameTable<Creature2Entry>();
        var files = new List<object>();
        foreach (Type type in loader.GetGameTables())
        {
            string relative = "tbl/" + type.Name[..^5] + ".tbl";
            byte[] bytes = File.ReadAllBytes(Path.Combine(recoveryRoot, relative));
            string hash = Convert.ToHexString(SHA256.HashData(bytes));
            Assert.Equal(inventory.RootElement.GetProperty("Files").GetProperty(relative).GetString(), hash);
            files.Add(new { File = relative, Sha256 = hash, Bytes = bytes.Length });
        }
        var tables = new GameTableManager(Options.Create(new GameTableConfig { GameTablePath = Path.Combine(recoveryRoot, "tbl") }));
        await tables.Initialise(loader);
        Assert.Equal(new uint[] { 4813, 0, 0, 0, 0, 0 }, tables.Quest2.GetEntry(3741).Objectives);
        var row = tables.QuestObjective.GetEntry(4813);
        Assert.Equal(32u, row.Type); Assert.Equal(4u, row.Flags);
        Assert.Equal(363u, row.Data); Assert.Equal(6u, row.Count);
        Assert.NotNull(tables.Creature2.GetEntry(12919));
        var test = new Q3741SupplyCrateCollectionTests(tables);
        var cases = new List<(string Name, Func<Task> Run)>();
        foreach (bool creature in new[] { false, true })
        {
            cases.Add(($"SixDistinctSupplyCrates/{creature}", () => { test.SixDistinctSupplyCratesAchieveTheRealQuestWithoutRepeatedCredit(creature); return Task.CompletedTask; }));
            cases.Add(($"RefusedDeletionRetry/{creature}", () => { test.RefusedPendingDeletionDoesNotConsumeTheSupplyCrateAndCanRetry(creature); return Task.CompletedTask; }));
            cases.Add(($"SatisfiedObjective/{creature}", () => { test.AlreadySatisfiedObjectiveDoesNotConsumeAnotherSupplyCrate(creature); return Task.CompletedTask; }));
            cases.Add(($"RemovalRetry/{creature}", () => { test.FailedRemovalRetriesTheOriginalObjectWithoutAnotherCredit(creature); return Task.CompletedTask; }));
        }
        cases.Add(("ReentrantObjectiveFeedback", () => { test.ReentrantObjectiveFeedbackCannotCollectTheSameSupplyCrateTwice(); return Task.CompletedTask; }));
        cases.Add(("IndependentLoadedOwner", () => { test.LoadingAnotherOwnerStartsAnIndependentPickup(); return Task.CompletedTask; }));
        cases.Add(("FailedPlayerSaveRetryReload", test.CollectedProgressSurvivesFullPlayerSaveFailureRetryAndFreshQuestReload));
        string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
        var json = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(outputDirectory, "inputs.json"), JsonSerializer.Serialize(new
        {
            ManifestSha256 = Hash(manifest), Files = files,
            ReaderSha256 = Hash(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location)),
            RuntimeSha256 = Hash(File.ReadAllBytes(typeof(NexusForever.Game.Quest.Quest).Assembly.Location)),
            ScriptSha256 = Hash(File.ReadAllBytes(typeof(Q3741ExileSupplyCrateCollectableEntityScript).Assembly.Location)),
            TestsSha256 = Hash(File.ReadAllBytes(typeof(Q3741SupplyCrateCollectionTests).Assembly.Location))
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
                entry = new Quest2Entry { Id = 3741, Objectives = [4813, 0, 0, 0, 0, 0], PushedItemIds = new uint[6], PushedItemCounts = new uint[6] };
                proxy.SetProperty(nameof(IGameTableManager.QuestObjective), NexusForever.Game.Tests.Transport.PlayerBindPointTests.Table(
                    new QuestObjectiveEntry { Id = 4813, Type = 32, Flags = 4, Data = 363, Count = 6 }));
            }
            else
            {
                foreach (var property in typeof(IGameTableManager).GetProperties())
                    if (property.GetValue(actual) is { } value) proxy.SetProperty(property.Name, value);
                entry = actual.Quest2.GetEntry(3741);
            }
            var info = new QuestInfo(entry, Storage.Global, Storage.Source.Tables);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)Storage.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestInfo), info);
            using (var seed = Storage.Context())
            {
                seed.CharacterQuest.Add(new() { Id = 42, QuestId = 3741, State = (byte)QuestState.Accepted,
                    QuestObjective = new List<CharacterQuestObjectiveModel> { new() { Id = 42, QuestId = 3741, Index = 0 } } });
                seed.SaveChanges();
            }
            using var read = Storage.Context();
            Actor = new(Storage, actorModel: read.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single());
            Quest = Assert.IsType<NexusForever.Game.Quest.Quest>(Assert.Single(Actor.Quests.GetActiveQuests()));
        }
        internal (Q3741ExileSupplyCrateEntityScriptBase Script, RecordingDispatchProxy<ISupplyCrateEntity> Owner) Pickup(bool creature)
        {
            var owner = RecordingDispatchProxy<ISupplyCrateEntity>.Create(out var proxy);
            if (creature)
            {
                var script = new Q3741ExileSupplyCrateEntityScript();
                script.OnLoad(owner);
                return (script, proxy);
            }
            else
            {
                var script = new Q3741ExileSupplyCrateCollectableEntityScript();
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
            if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned Q3741 Character flush refusal.");
            return context;
        }, 42, Actor.Player.Save);
        public void Dispose() { Actor.Dispose(); Storage.Dispose(); }
    }
}
