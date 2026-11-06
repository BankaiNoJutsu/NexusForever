using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NexusForever.Database.Character;
using NexusForever.Database.Character.Model;
using NexusForever.Game.Abstract.Achievement;
using NexusForever.Game.Abstract.Entity;
using NexusForever.Game.Abstract.Quest;
using NexusForever.Game.Abstract.RBAC;
using NexusForever.Game.Achievement;
using NexusForever.Game.Entity;
using NexusForever.Game.Quest;
using NexusForever.Game.Static.Entity;
using NexusForever.Game.Static.Quest;
using NexusForever.Game.Tests.TestSupport;
using NexusForever.GameTable;
using NexusForever.GameTable.Model;
using Xunit.Abstractions;
using Actor = NexusForever.Game.Tests.Challenges.RewardNormalSaveSnapshotTests.Actor;
using Fixture = NexusForever.Game.Tests.Quest.QuestPersistenceDurabilityTests.Fixture;

namespace NexusForever.Game.Tests.Quest;

public static class Q5597ActualCatalogQualification
{
    public static Task RunAsync(string recoveryRoot, string outputDirectory)
        => Q5597RewardPersistenceTests.RunActualCatalogAsync(recoveryRoot, outputDirectory);
}

public sealed partial class Q5597RewardPersistenceTests
{
    // Explicit offline entry point. Shipped tables remain outside Git and xUnit discovery.
    internal static async Task RunActualCatalogAsync(string recoveryRoot, string outputDirectory)
    {
        recoveryRoot = Path.GetFullPath(recoveryRoot);
        outputDirectory = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(outputDirectory)) throw new IOException("Retain prior evidence; choose a new output directory.");
        Directory.CreateDirectory(outputDirectory);
        var json = new JsonSerializerOptions { WriteIndented = true };
        var started = DateTime.UtcNow;
        try
        {
            var (manifest, inputs) = ContractActualCatalogQualification.VerifyInputs(recoveryRoot);
            var tables = await ContractActualCatalogQualification.LoadTablesAsync(recoveryRoot);
            await tables.Initialise(new GameTableLoader()
                .AddGameTable<Quest2Entry>().AddGameTable<Quest2DifficultyEntry>()
                .AddGameTable<QuestObjectiveEntry>().AddGameTable<Quest2RewardEntry>()
                .AddGameTable<AchievementEntry>().AddGameTable<AchievementChecklistEntry>()
                .AddGameTable<PrerequisiteEntry>().AddGameTable<XpPerLevelEntry>());
            var catalog = new ActualCatalog(tables);
            File.WriteAllText(Path.Combine(outputDirectory, "inputs.json"), JsonSerializer.Serialize(new
            {
                ManifestSha256 = Hash(manifest), Files = inputs,
                ReaderSha256 = Hash(File.ReadAllBytes(typeof(GameTable<>).Assembly.Location)),
                RuntimeSha256 = Hash(File.ReadAllBytes(typeof(ItemInfo).Assembly.Location)),
                ProbeSha256 = Hash(File.ReadAllBytes(typeof(Q5597RewardPersistenceTests).Assembly.Location)),
                catalog.InitialXp, RewardXp = catalog.Quest.GetRewardExperience(), RewardCredits = catalog.Quest.GetRewardMoney(),
                Checklist = catalog.Achievement.ChecklistEntries.Select(row => new { row.Id, row.Bit, row.ObjectId })
            }, json));

            using (var f = new Fixture(null))
            {
                using (var seed = f.Context())
                {
                    catalog.Seed(seed);
                    seed.CharacterQuest.Add(new() { Id = 42, QuestId = QuestId, State = (byte)QuestState.Achieved });
                    await seed.SaveChangesAsync();
                }
                catalog.Bind(f);
                using var actor = LoadActor(f, CreateItems(tables), 1, actualAchievement: catalog.Achievement);
                await actor.Load();
                actor.Quests.QuestComplete(QuestId, 0, false);
                Assert.Equal(QuestState.Achieved, actor.Quests.GetQuestState(QuestId));
                Assert.Empty(actor.Player.Inventory.SelectMany(bag => bag));
                using var read = f.Context();
                Assert.Equal((byte)QuestState.Achieved, Assert.Single(read.CharacterQuest).State);
                Assert.Empty(read.Item);
                catalog.AssertPersisted(read.Character.AsNoTracking().Include(row => row.Achievement).Include(row => row.Currency).Single(), false);
            }

            var probe = new Q5597RewardPersistenceTests(new QualificationOutput());
            await probe.RunQuestLoopback(false, actualCatalog: catalog);
            await probe.RunQuestLoopback(true, actualCatalog: catalog);
            Assert.Equal(2, catalog.Results.Count);
            File.WriteAllText(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(new
            {
                CompletedUtc = DateTime.UtcNow, ElapsedSeconds = (DateTime.UtcNow - started).TotalSeconds,
                ActualCapacityRefusal = true, LoopbackCases = catalog.Results,
                Limit = "Achieved quest and prior checklist bit are seeded; prerequisite18 is a satisfied collaborator. Prepared World actor/receiver/bootstrap, SQLite and local encrypted sockets; no real earning, Auth selection, physical chain, native UI or retail qualification."
            }, json));
        }
        catch (Exception error)
        {
            File.WriteAllText(Path.Combine(outputDirectory, "failure.json"), JsonSerializer.Serialize(new { Error = error.ToString() }, json));
            throw;
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    private sealed class QualificationOutput : ITestOutputHelper
    {
        public void WriteLine(string message) => Console.WriteLine(message);
        public void WriteLine(string format, params object[] args) => Console.WriteLine(format, args);
    }

    private sealed class ActualCatalog
    {
        internal readonly GameTableManager Tables;
        internal readonly QuestInfo Quest;
        internal readonly IAchievementInfo Achievement;
        internal readonly uint InitialXp;
        internal readonly List<object> Results = [];

        internal ActualCatalog(GameTableManager tables)
        {
            Tables = tables;
            Quest = new QuestInfo(tables.Quest2.GetEntry(QuestId), gameTableManager: tables);
            Achievement = new AchievementInfo(tables.Achievement.GetEntry(AchievementId), tables);
            Assert.Equal(new uint[] { 5494, 5495, 5496 }, Achievement.ChecklistEntries.OrderBy(row => row.Id).Select(row => row.Id));
            Assert.Equal(new uint[] { 5596, 5597, 5604 }, Achievement.ChecklistEntries.OrderBy(row => row.Bit).Select(row => row.ObjectId));
            Assert.Equal(1u, Achievement.ChecklistEntries.Single(row => row.ObjectId == QuestId).Bit);
            Assert.Equal(new uint[] { 3000, 5236 }, Quest.Rewards.Keys.Order());
            Assert.Equal(RewardIds, Quest.Rewards.Values.Select(row => row.ObjectId).Order());
            Assert.All(Quest.Rewards.Values, row => { Assert.Equal(1u, row.ObjectAmount); Assert.Equal(0u, row.Flags); });
            InitialXp = tables.XpPerLevel.GetEntry(4).MinXpForLevel;
            Assert.True(Quest.GetRewardExperience() > 0);
            Assert.True(Quest.GetRewardMoney() > 0);
            Assert.True(InitialXp + Quest.GetRewardExperience() < tables.XpPerLevel.GetEntry(5).MinXpForLevel);
        }

        internal void Seed(CharacterContext context)
        {
            var character = context.Character.Single();
            character.Level = 4; character.TotalXp = InitialXp; character.LastOnline = null;
            context.CharacterStat.Add(new() { Id = character.Id, Stat = (byte)Stat.Level, Value = character.Level });
            context.CharacterQuestObjective.Add(new() { Id = character.Id, QuestId = QuestId, Index = 0,
                Progress = Quest.Objectives.Single().Entry.Count });
            context.CharacterAchievement.Add(new() { Id = character.Id, AchievementId = AchievementId, ProgressState = 1 });
        }

        internal void Bind(Fixture f)
        {
            var tables = (RecordingDispatchProxy<IGameTableManager>)(object)f.Source.Tables;
            foreach (var property in typeof(IGameTableManager).GetProperties())
            {
                object value = property.GetValue(Tables);
                if (value != null) tables.SetProperty(property.Name, value);
            }
            var info = new QuestInfo(Tables.Quest2.GetEntry(QuestId), f.Global, Tables);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)f.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestInfo), info);
            ((RecordingDispatchProxy<IGlobalQuestManager>)(object)f.Global).SetMethodReturn(nameof(IGlobalQuestManager.GetQuestReceivers), new[] { Receiver });
        }

        internal void PrepareActor(Actor actor, CharacterModel loaded)
        {
            var stats = (Dictionary<Stat, IStatValue>)typeof(WorldEntity).GetField("stats", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(actor.Player)!;
            stats[Stat.Level] = new StatValue(new CharacterStatModel { Id = loaded.Id, Stat = (byte)Stat.Level, Value = loaded.Level });
            ((RecordingDispatchProxy<NexusForever.Game.Abstract.Account.IAccount>)(object)actor.Player.Account)
                .SetProperty("RbacManager", RecordingDispatchProxy<IAccountRBACManager>.Create(out _));
            actor.Set(nameof(Player.XpManager), new XpManager(actor.Player, loaded, Tables));
            var wallet = new CurrencyManager(Tables);
            wallet.Initialise(actor.Player, loaded);
            actor.Set(nameof(Player.CurrencyManager), wallet);
        }

        internal void AssertPersisted(CharacterModel model, bool completed)
        {
            Assert.Equal(4, model.Level);
            Assert.Equal(InitialXp + (completed ? Quest.GetRewardExperience() : 0), model.TotalXp);
            var progress = Assert.Single(model.Achievement);
            Assert.Equal(completed ? 3u : 1u, progress.ProgressState);
            Assert.Null(progress.DateCompleted);
            if (completed)
            {
                var credits = Assert.Single(model.Currency);
                Assert.Equal((byte)CurrencyType.Credits, credits.CurrencyId);
                Assert.Equal((ulong)Quest.GetRewardMoney(), credits.Amount);
            }
            else Assert.Empty(model.Currency);
        }
    }
}
