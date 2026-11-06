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
using NexusForever.Script.Main.Quests.NorthernWilds;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Store = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public sealed class Q4526SequentialCatchTests
{
    private readonly IGameTableManager actualTables;
    public Q4526SequentialCatchTests() { }
    private Q4526SequentialCatchTests(IGameTableManager tables) => actualTables = tables;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EarlyCatchCanRetryTheSameAnomalyAfterWreckageInvestigation(bool activate)
    {
        using var f = new CatchFixture(actualTables);
        var script = f.Anomaly();
        f.Catch(script, activate);
        Assert.Equal(0u, f.Objective(6165).Progress);
        Assert.Equal(QuestState.Accepted, f.Quest.State);
        f.Quest.ObjectiveUpdate(6164, 1);
        Assert.Equal(6165u, f.Quest.GetCurrentObjectiveId());
        f.Catch(script, activate);
        Assert.Equal(1u, f.Objective(6165).Progress);
        Assert.Equal(QuestState.Achieved, f.Quest.State);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ValidCatchCompletesOnceAcrossBothEntryPoints(bool activate)
    {
        using var f = new CatchFixture(actualTables, investigated: true);
        var script = f.Anomaly();
        f.Catch(script, activate);
        f.Catch(script, !activate);
        Assert.Equal(1u, f.Objective(6165).Progress);
        Assert.Equal(QuestState.Achieved, f.Quest.State);
    }

    [Fact]
    public void AbandonedQuestCannotReceiveCatchCredit()
    {
        using var f = new CatchFixture(actualTables, investigated: true);
        f.Quest.State = QuestState.Abandoned;
        f.Catch(f.Anomaly(), false);
        Assert.Equal(0u, f.Objective(6165).Progress);
    }

    [Fact]
    public async Task RetriedCatchSurvivesFullPlayerSaveFailureRetryAndFreshQuestReload()
    {
        using var f = new CatchFixture(actualTables);
        var script = f.Anomaly();
        f.Catch(script, false);
        f.Quest.ObjectiveUpdate(6164, 1);
        f.Catch(script, true);
        Assert.Equal(QuestState.Achieved, f.Quest.State);
        await Assert.ThrowsAsync<IOException>(() => f.Persist(true));
        await f.Persist();
        using var read = f.Storage.Context();
        Assert.Equal((byte)QuestState.Achieved, Assert.Single(read.CharacterQuest).State);
        Assert.Equal(1u, read.CharacterQuestObjective.Single(row => row.Index == 1).Progress);
        using var fresh = f.Reload();
        var quest = Assert.Single(fresh.Quests.GetActiveQuests());
        Assert.Equal(QuestState.Achieved, quest.State);
        script.OnEnterRange(fresh.Player);
        Assert.Equal(1u, Assert.Single(quest, objective => objective.ObjectiveInfo.Id == 6165).Progress);
    }

    // Explicit offline qualification using the retained recovered corpus.
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
        Assert.Equal(new uint[] { 6164, 6165, 0, 0, 0, 0 }, tables.Quest2.GetEntry(4526).Objectives);
        foreach (var expected in new[] { (6164u, 0u), (6165u, 3u) })
        {
            var row = tables.QuestObjective.GetEntry(expected.Item1);
            Assert.Equal(5u, row.Type); Assert.Equal(expected.Item2, row.Flags);
            Assert.Equal(0u, row.Data); Assert.Equal(1u, row.Count);
        }
        Assert.NotNull(tables.Creature2.GetEntry(12535));
        var test = new Q4526SequentialCatchTests(tables);
        var cases = new List<(string Name, Func<Task> Run)>
        {
            ("EarlyRangeRetry", () => { test.EarlyCatchCanRetryTheSameAnomalyAfterWreckageInvestigation(false); return Task.CompletedTask; }),
            ("EarlyActivationRetry", () => { test.EarlyCatchCanRetryTheSameAnomalyAfterWreckageInvestigation(true); return Task.CompletedTask; }),
            ("ValidRangeOnce", () => { test.ValidCatchCompletesOnceAcrossBothEntryPoints(false); return Task.CompletedTask; }),
            ("ValidActivationOnce", () => { test.ValidCatchCompletesOnceAcrossBothEntryPoints(true); return Task.CompletedTask; }),
            ("AbandonedQuest", () => { test.AbandonedQuestCannotReceiveCatchCredit(); return Task.CompletedTask; }),
            ("FailedPlayerSaveRetryReload", test.RetriedCatchSurvivesFullPlayerSaveFailureRetryAndFreshQuestReload)
        };
        string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));
        var json = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path.Combine(outputDirectory, "inputs.json"), JsonSerializer.Serialize(new
        {
            ManifestSha256 = Hash(manifest), Files = files,
            ReaderSha256 = Hash(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location)),
            RuntimeSha256 = Hash(File.ReadAllBytes(typeof(NexusForever.Game.Quest.Quest).Assembly.Location)),
            ScriptSha256 = Hash(File.ReadAllBytes(typeof(Q4526SpatialAnomalyEntityScript).Assembly.Location)),
            TestsSha256 = Hash(File.ReadAllBytes(typeof(Q4526SequentialCatchTests).Assembly.Location))
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
            Limit = "Real script and Player/Quest SQLite save/reload with actual recovered QuestInfo. Accepted quest and first objective are prepared; no public NPC acceptance/turnin, map/movement/native catch or retail qualification."
        }, json));
    }

    internal sealed class CatchFixture : IDisposable
    {
        internal readonly Store Storage = new(null);
        internal readonly Actor Actor;
        internal readonly NexusForever.Game.Quest.Quest Quest;
        internal CatchFixture(IGameTableManager actual = null, bool investigated = false)
        {
            var proxy = (RecordingDispatchProxy<IGameTableManager>)(object)Storage.Source.Tables;
            Quest2Entry entry;
            if (actual == null)
            {
                entry = new Quest2Entry { Id = 4526, Objectives = [6164, 6165, 0, 0, 0, 0], PushedItemIds = new uint[3], PushedItemCounts = new uint[3] };
                proxy.SetProperty(nameof(IGameTableManager.QuestObjective), NexusForever.Game.Tests.Transport.PlayerBindPointTests.Table(
                    new QuestObjectiveEntry { Id = 6164, Type = 5, Count = 1 },
                    new QuestObjectiveEntry { Id = 6165, Type = 5, Flags = 3, Count = 1 }));
            }
            else
            {
                foreach (var property in typeof(IGameTableManager).GetProperties())
                    if (property.GetValue(actual) is { } value) proxy.SetProperty(property.Name, value);
                entry = actual.Quest2.GetEntry(4526);
            }
            var info = new QuestInfo(entry, Storage.Global, Storage.Source.Tables);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)Storage.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestInfo), info);
            using (var seed = Storage.Context())
            {
                seed.CharacterQuest.Add(new() { Id = 42, QuestId = 4526, State = (byte)QuestState.Accepted,
                    QuestObjective = new List<CharacterQuestObjectiveModel> {
                        new() { Id = 42, QuestId = 4526, Index = 0, Progress = investigated ? 1u : 0u },
                        new() { Id = 42, QuestId = 4526, Index = 1 } } });
                seed.SaveChanges();
            }
            using var read = Storage.Context();
            Actor = new(Storage, actorModel: read.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single());
            Quest = Assert.IsType<NexusForever.Game.Quest.Quest>(Assert.Single(Actor.Quests.GetActiveQuests()));
        }
        internal IQuestObjective Objective(uint id) => Quest.Single(objective => objective.ObjectiveInfo.Id == id);
        internal Q4526SpatialAnomalyEntityScript Anomaly()
        {
            var owner = RecordingDispatchProxy<ICreatureEntity>.Create(out var entity);
            entity.SetProperty(nameof(ICreatureEntity.Guid), 5000u);
            entity.SetProperty(nameof(ICreatureEntity.CreatureId), 12535u);
            var script = new Q4526SpatialAnomalyEntityScript(); script.OnLoad(owner);
            return script;
        }
        internal void Catch(Q4526SpatialAnomalyEntityScript script, bool activate)
        {
            if (activate) script.OnActivateSuccess(Actor.Player);
            else script.OnEnterRange(Actor.Player);
        }
        internal Actor Reload()
        {
            using var read = Storage.Context();
            return new(Storage, actorModel: read.Character.AsNoTracking().Include(row => row.Quest).ThenInclude(row => row.QuestObjective).Single());
        }
        internal Task Persist(bool fail = false) => CharacterAtomicSave.Execute(() =>
        {
            var context = Storage.Context();
            if (fail) context.SavingChanges += (_, _) => throw new IOException("Owned Q4526 Character flush refusal.");
            return context;
        }, 42, Actor.Player.Save);
        public void Dispose() { Actor.Dispose(); Storage.Dispose(); }
    }
}
